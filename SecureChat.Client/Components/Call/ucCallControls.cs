using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SecureChat.Client.Components.Call
{
    public partial class ucCallControls : UserControl
    {
        private readonly SemaphoreSlim _actionLock = new(1, 1);
        private readonly CancellationTokenSource _lifetimeCts = new();
        private readonly object _eventGate = new();
        private readonly HashSet<CallControlAction> _oneShotRaised = new();

        private object? _signalRClient;
        private object? _videoHandler;
        private object? _audioHandler;
        private bool _ownsMediaHandlers;
        private bool _ownsSignalRClient;

        private string _callId = string.Empty;
        private bool _micMuted;
        private bool _cameraEnabled = true;
        private bool _mediaStarted;
        private int _closeEventRaised;
        private int _disposedFlag;
        private CallUiState _state = CallUiState.Incoming;

        private Panel _root = null!;
        private Panel _bottomHost = null!;
        private TableLayoutPanel _buttonsLayout = null!;

        private CallCircleButton _btnAccept = null!;
        private CallCircleButton _btnReject = null!;
        private CallCircleButton _btnEnd = null!;
        private CallCircleButton _btnMic = null!;
        private CallCircleButton _btnCamera = null!;

        private Label _lblAccept = null!;
        private Label _lblReject = null!;
        private Label _lblEnd = null!;
        private Label _lblMic = null!;
        private Label _lblCamera = null!;

        public event EventHandler<CallActionEventArgs>? ActionExecuted;
        public event EventHandler? CallCloseRequested;
        public event EventHandler<Exception>? OperationFailed;

        public ucCallControls()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BuildUi();
            SetUiState(CallUiState.Incoming);
            Disposed += (_, __) => CleanupLifetimeResources();
        }

        public void ConfigureServices(
            object? signalRClient,
            object? videoHandler,
            object? audioHandler,
            string? callId = null,
            bool ownsMediaHandlers = false,
            bool ownsSignalRClient = false)
        {
            _signalRClient = signalRClient;
            _videoHandler = videoHandler;
            _audioHandler = audioHandler;
            _callId = callId ?? _callId;
            _ownsMediaHandlers = ownsMediaHandlers;
            _ownsSignalRClient = ownsSignalRClient;
        }

        public void SetCallId(string callId) => _callId = callId ?? string.Empty;

        public void SetUiState(CallUiState state)
        {
            _state = state;

            bool incoming = state == CallUiState.Incoming;
            bool inCall = state == CallUiState.InCall;
            bool ended = state == CallUiState.Ended;

            _btnAccept.Visible = incoming;
            _lblAccept.Visible = incoming;
            _btnReject.Visible = incoming;
            _lblReject.Visible = incoming;

            _btnEnd.Visible = inCall;
            _lblEnd.Visible = inCall;
            _btnMic.Visible = inCall;
            _lblMic.Visible = inCall;
            _btnCamera.Visible = inCall;
            _lblCamera.Visible = inCall;

            Enabled = !ended;
            ReflowButtonsForCurrentState();
            Invalidate();
        }

        private void ReflowButtonsForCurrentState()
        {
            if (_buttonsLayout == null) return;

            if (_state == CallUiState.Incoming)
            {
                // Center 2 buttons (Accept/Reject)
                _buttonsLayout.SetColumn(_btnAccept, 1);
                _buttonsLayout.SetColumn(_lblAccept, 1);
                _buttonsLayout.SetColumn(_btnReject, 3);
                _buttonsLayout.SetColumn(_lblReject, 3);
                return;
            }

            if (_state == CallUiState.InCall)
            {
                // Center 3 buttons (Camera/End/Mic)
                _buttonsLayout.SetColumn(_btnCamera, 1);
                _buttonsLayout.SetColumn(_lblCamera, 1);
                _buttonsLayout.SetColumn(_btnEnd, 2);
                _buttonsLayout.SetColumn(_lblEnd, 2);
                _buttonsLayout.SetColumn(_btnMic, 3);
                _buttonsLayout.SetColumn(_lblMic, 3);
                return;
            }

            // Default/fallback layout
            _buttonsLayout.SetColumn(_btnAccept, 0);
            _buttonsLayout.SetColumn(_lblAccept, 0);
            _buttonsLayout.SetColumn(_btnReject, 1);
            _buttonsLayout.SetColumn(_lblReject, 1);
            _buttonsLayout.SetColumn(_btnEnd, 2);
            _buttonsLayout.SetColumn(_lblEnd, 2);
            _buttonsLayout.SetColumn(_btnMic, 3);
            _buttonsLayout.SetColumn(_lblMic, 3);
            _buttonsLayout.SetColumn(_btnCamera, 4);
            _buttonsLayout.SetColumn(_lblCamera, 4);
        }

        public async Task ApplyRemoteSignalAsync(string signal, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(signal)) return;
            if (IsDisposedState()) return;

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token, cancellationToken);
            string normalized = signal.Trim().ToUpperInvariant();

            await _actionLock.WaitAsync(linked.Token).ConfigureAwait(false);
            try
            {
                if (_state == CallUiState.Ended)
                    return;

                switch (normalized)
                {
                    case "CALL_ACCEPTED":
                        if (_state != CallUiState.InCall)
                        {
                            await StartMediaAsync(linked.Token).ConfigureAwait(false);
                            RunOnUi(() => SetUiState(CallUiState.InCall));
                        }
                        RaiseAction(CallControlAction.RemoteAccepted, oneShot: true);
                        break;

                    case "CALL_REJECTED":
                        await StopMediaAsync(linked.Token).ConfigureAwait(false);
                        RunOnUi(() => SetUiState(CallUiState.Ended));
                        RaiseAction(CallControlAction.RemoteRejected, oneShot: true);
                        RaiseCloseRequestedOnce();
                        break;

                    case "CALL_ENDED":
                        await StopMediaAsync(linked.Token).ConfigureAwait(false);
                        RunOnUi(() => SetUiState(CallUiState.Ended));
                        RaiseAction(CallControlAction.RemoteEnded, oneShot: true);
                        RaiseCloseRequestedOnce();
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // ignore cancellation during shutdown
            }
            catch (Exception ex)
            {
                HandleOperationError(ex);
            }
            finally
            {
                _actionLock.Release();
            }
        }

        private void BuildUi()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);

            _root = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            EnableDoubleBufferRecursive(_root);
            Controls.Add(_root);

            _bottomHost = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = ScalePx(118),
                BackColor = Color.Transparent
            };
            _root.Controls.Add(_bottomHost);

            _buttonsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.None,
                ColumnCount = 5,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _buttonsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, ScalePx(74)));
            _buttonsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, ScalePx(28)));
            for (int i = 0; i < 5; i++)
            {
                _buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            }

            _bottomHost.Controls.Add(_buttonsLayout);
            _bottomHost.Resize += (_, __) => LayoutButtonsBottomCenter();

            _btnAccept = BuildButton(ButtonKind.Accept);
            _btnReject = BuildButton(ButtonKind.Reject);
            _btnEnd = BuildButton(ButtonKind.End);
            _btnMic = BuildButton(ButtonKind.Mic);
            _btnCamera = BuildButton(ButtonKind.Camera);

            _lblAccept = BuildLabel("Accept");
            _lblReject = BuildLabel("Reject");
            _lblEnd = BuildLabel("End");
            _lblMic = BuildLabel("Mute");
            _lblCamera = BuildLabel("Camera");

            AddColumn(0, _btnAccept, _lblAccept);
            AddColumn(1, _btnReject, _lblReject);
            AddColumn(2, _btnEnd, _lblEnd);
            AddColumn(3, _btnMic, _lblMic);
            AddColumn(4, _btnCamera, _lblCamera);

            _btnAccept.Click += async (_, __) => await ExecuteActionAsync(CallControlAction.Accept);
            _btnReject.Click += async (_, __) => await ExecuteActionAsync(CallControlAction.Reject);
            _btnEnd.Click += async (_, __) => await ExecuteActionAsync(CallControlAction.End);
            _btnMic.Click += async (_, __) => await ExecuteActionAsync(CallControlAction.ToggleMic);
            _btnCamera.Click += async (_, __) => await ExecuteActionAsync(CallControlAction.ToggleCamera);

            LayoutButtonsBottomCenter();
            _buttonsLayout.Resize += (_, __) => ReflowButtonsForCurrentState();
        }

        private void LayoutButtonsBottomCenter()
        {
            int w = ScalePx(420);
            int h = ScalePx(102);
            _buttonsLayout.Size = new Size(w, h);
            _buttonsLayout.Location = new Point((_bottomHost.Width - w) / 2, Math.Max(0, _bottomHost.Height - h));
        }

        private void AddColumn(int column, Control top, Control bottom)
        {
            top.Anchor = AnchorStyles.None;
            bottom.Anchor = AnchorStyles.Top;
            _buttonsLayout.Controls.Add(top, column, 0);
            _buttonsLayout.Controls.Add(bottom, column, 1);
        }

        private async Task ExecuteActionAsync(CallControlAction action)
        {
            if (IsDisposedState()) return;

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);

            await _actionLock.WaitAsync(linked.Token).ConfigureAwait(false);
            try
            {
                switch (action)
                {
                    case CallControlAction.Accept:
                        if (_state == CallUiState.Ended || _state == CallUiState.InCall) return;
                        RunOnUi(() => SetUiState(CallUiState.Ringing));
                        await SendSignalAsync("CALL_ACCEPTED", linked.Token).ConfigureAwait(false);
                        await StartMediaAsync(linked.Token).ConfigureAwait(false);
                        RunOnUi(() => SetUiState(CallUiState.InCall));
                        break;

                    case CallControlAction.Reject:
                        if (_state == CallUiState.Ended) return;
                        await SendSignalAsync("CALL_REJECTED", linked.Token).ConfigureAwait(false);
                        await StopMediaAsync(linked.Token).ConfigureAwait(false);
                        RunOnUi(() =>
                        {
                            SetUiState(CallUiState.Ended);
                        });
                        RaiseCloseRequestedOnce();
                        break;

                    case CallControlAction.End:
                        if (_state == CallUiState.Ended) return;
                        await StopMediaAsync(linked.Token).ConfigureAwait(false);
                        await SendSignalAsync("CALL_ENDED", linked.Token).ConfigureAwait(false);
                        RunOnUi(() =>
                        {
                            SetUiState(CallUiState.Ended);
                        });
                        RaiseCloseRequestedOnce();
                        break;

                    case CallControlAction.ToggleMic:
                        if (_state != CallUiState.InCall) return;
                        _micMuted = !_micMuted;
                        await SetMicrophoneMuteAsync(_micMuted, linked.Token).ConfigureAwait(false);
                        RunOnUi(() =>
                        {
                            _btnMic.IsToggled = _micMuted;
                            _lblMic.Text = _micMuted ? "Unmute" : "Mute";
                            _btnMic.Invalidate();
                        });
                        break;

                    case CallControlAction.ToggleCamera:
                        if (_state != CallUiState.InCall) return;
                        _cameraEnabled = !_cameraEnabled;
                        await SetCameraEnabledAsync(_cameraEnabled, linked.Token).ConfigureAwait(false);
                        RunOnUi(() =>
                        {
                            _btnCamera.IsToggled = !_cameraEnabled;
                            _lblCamera.Text = _cameraEnabled ? "Camera" : "Cam Off";
                            _btnCamera.Invalidate();
                        });
                        break;
                }

                RaiseAction(action, oneShot: action is CallControlAction.Accept or CallControlAction.Reject or CallControlAction.End);
            }
            catch (OperationCanceledException)
            {
                // ignore cancellation during shutdown
            }
            catch (Exception ex)
            {
                if (action == CallControlAction.Accept && _state == CallUiState.Ringing)
                    RunOnUi(() => SetUiState(CallUiState.Incoming));
                HandleOperationError(ex);
            }
            finally
            {
                _actionLock.Release();
            }
        }

        private async Task SendSignalAsync(string signal, CancellationToken ct)
        {
            if (_signalRClient == null)
                throw new InvalidOperationException("SignalRClient is not configured.");

            if (!IsSignalRConnected(_signalRClient))
                throw new InvalidOperationException("SignalR connection is not active.");

            var methodNames = new[]
            {
                "SendCallSignalAsync",
                "SendCallControlAsync",
                "SendSignalAsync",
                "SendAsync"
            };

            var argSets = new List<object?[]>
            {
                new object?[] { _callId, signal },
                new object?[] { signal, _callId },
                new object?[] { signal }
            };

            bool invoked = await TryInvokeAnyAsync(_signalRClient, methodNames, argSets, ct).ConfigureAwait(false);
            if (!invoked)
                throw new MissingMethodException("No compatible signaling method found on SignalRClient.");
        }

        private async Task StartMediaAsync(CancellationToken ct)
        {
            if (_mediaStarted) return;

            if (_videoHandler != null)
            {
                bool okVideo = await TryInvokeAnyAsync(
                    _videoHandler,
                    new[] { "StartAsync", "StartCaptureAsync", "StartVideoAsync", "Start" },
                    new List<object?[]> { Array.Empty<object?>() },
                    ct).ConfigureAwait(false);

                if (!okVideo)
                    throw new MissingMethodException("No compatible start method found on VideoHandler.");
            }

            if (_audioHandler != null)
            {
                bool okAudio = await TryInvokeAnyAsync(
                    _audioHandler,
                    new[] { "StartAsync", "StartCaptureAsync", "StartAudioAsync", "Start" },
                    new List<object?[]> { Array.Empty<object?>() },
                    ct).ConfigureAwait(false);

                if (!okAudio)
                    throw new MissingMethodException("No compatible start method found on AudioHandler.");
            }

            _mediaStarted = true;
        }

        private async Task StopMediaAsync(CancellationToken ct)
        {
            if (!_mediaStarted && _state != CallUiState.Ringing && _state != CallUiState.InCall)
                return;

            if (_videoHandler != null)
            {
                await TryInvokeAnyAsync(
                    _videoHandler,
                    new[] { "StopAsync", "StopCaptureAsync", "StopVideoAsync", "Stop" },
                    new List<object?[]> { Array.Empty<object?>() },
                    ct).ConfigureAwait(false);
            }

            if (_audioHandler != null)
            {
                await TryInvokeAnyAsync(
                    _audioHandler,
                    new[] { "StopAsync", "StopCaptureAsync", "StopAudioAsync", "Stop" },
                    new List<object?[]> { Array.Empty<object?>() },
                    ct).ConfigureAwait(false);
            }

            _mediaStarted = false;
        }

        private async Task SetMicrophoneMuteAsync(bool muted, CancellationToken ct)
        {
            if (_audioHandler == null)
                throw new InvalidOperationException("AudioHandler is not configured.");

            if (muted)
            {
                bool ok = await TryInvokeAnyAsync(
                    _audioHandler,
                    new[] { "SetMutedAsync", "MuteAsync", "DisableInputAsync", "SetMuteAsync" },
                    new List<object?[]> { new object?[] { true }, Array.Empty<object?>() },
                    ct).ConfigureAwait(false);

                if (!ok)
                    throw new MissingMethodException("No compatible mute method found on AudioHandler.");
            }
            else
            {
                bool ok = await TryInvokeAnyAsync(
                    _audioHandler,
                    new[] { "SetMutedAsync", "UnmuteAsync", "EnableInputAsync", "SetMuteAsync" },
                    new List<object?[]> { new object?[] { false }, Array.Empty<object?>() },
                    ct).ConfigureAwait(false);

                if (!ok)
                    throw new MissingMethodException("No compatible unmute method found on AudioHandler.");
            }
        }

        private async Task SetCameraEnabledAsync(bool enabled, CancellationToken ct)
        {
            if (_videoHandler == null)
                throw new InvalidOperationException("VideoHandler is not configured.");

            if (enabled)
            {
                bool ok = await TryInvokeAnyAsync(
                    _videoHandler,
                    new[] { "SetEnabledAsync", "EnableAsync", "SetVideoEnabledAsync", "ResumeAsync" },
                    new List<object?[]> { new object?[] { true }, Array.Empty<object?>() },
                    ct).ConfigureAwait(false);

                if (!ok)
                    throw new MissingMethodException("No compatible camera-enable method found on VideoHandler.");
            }
            else
            {
                bool ok = await TryInvokeAnyAsync(
                    _videoHandler,
                    new[] { "SetEnabledAsync", "DisableAsync", "SetVideoEnabledAsync", "PauseAsync" },
                    new List<object?[]> { new object?[] { false }, Array.Empty<object?>() },
                    ct).ConfigureAwait(false);

                if (!ok)
                    throw new MissingMethodException("No compatible camera-disable method found on VideoHandler.");
            }
        }

        private static bool IsSignalRConnected(object signalRClient)
        {
            var type = signalRClient.GetType();
            var prop = type.GetProperty("IsConnected", BindingFlags.Instance | BindingFlags.Public)
                       ?? type.GetProperty("Connected", BindingFlags.Instance | BindingFlags.Public)
                       ?? type.GetProperty("IsOnline", BindingFlags.Instance | BindingFlags.Public);

            if (prop?.PropertyType == typeof(bool))
            {
                try
                {
                    return (bool)(prop.GetValue(signalRClient) ?? false);
                }
                catch { return false; }
            }

            // If connectivity property does not exist, assume caller manages connection state.
            return true;
        }

        private static async Task<bool> TryInvokeAnyAsync(
            object target,
            IEnumerable<string> methodNames,
            IReadOnlyList<object?[]> candidateArguments,
            CancellationToken ct)
        {
            foreach (var methodName in methodNames)
            {
                var methods = target.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal))
                    .ToArray();

                foreach (var method in methods)
                {
                    foreach (var args in candidateArguments)
                    {
                        if (TryBuildInvokeArgs(method, args, ct, out var invokeArgs))
                        {
                            object? result;
                            try
                            {
                                result = method.Invoke(target, invokeArgs);
                            }
                            catch
                            {
                                continue;
                            }

                            if (result is Task task)
                                await task.ConfigureAwait(false);

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool TryBuildInvokeArgs(MethodInfo method, object?[] args, CancellationToken ct, out object?[] invokeArgs)
        {
            var ps = method.GetParameters();

            if (ps.Length == args.Length)
            {
                if (!CanMap(ps, args))
                {
                    invokeArgs = Array.Empty<object?>();
                    return false;
                }

                invokeArgs = args;
                return true;
            }

            if (ps.Length == args.Length + 1 && ps[^1].ParameterType == typeof(CancellationToken))
            {
                if (!CanMap(ps.Take(ps.Length - 1).ToArray(), args))
                {
                    invokeArgs = Array.Empty<object?>();
                    return false;
                }

                invokeArgs = args.Concat(new object?[] { ct }).ToArray();
                return true;
            }

            invokeArgs = Array.Empty<object?>();
            return false;
        }

        private static bool CanMap(ParameterInfo[] parameters, object?[] args)
        {
            if (parameters.Length != args.Length) return false;

            for (int i = 0; i < parameters.Length; i++)
            {
                var pType = parameters[i].ParameterType;
                var value = args[i];

                if (value == null)
                {
                    if (pType.IsValueType && Nullable.GetUnderlyingType(pType) == null)
                        return false;
                    continue;
                }

                if (!pType.IsAssignableFrom(value.GetType()))
                {
                    // Basic conversion for common primitives/string
                    try
                    {
                        Convert.ChangeType(value, pType);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void RaiseAction(CallControlAction action, bool oneShot = false)
        {
            if (oneShot)
            {
                lock (_eventGate)
                {
                    if (_oneShotRaised.Contains(action))
                        return;
                    _oneShotRaised.Add(action);
                }
            }

            RunOnUi(() => ActionExecuted?.Invoke(this, new CallActionEventArgs(action, _callId)));
        }

        private void HandleOperationError(Exception ex)
        {
            RunOnUi(() => OperationFailed?.Invoke(this, ex));
        }

        private void RunOnUi(Action action)
        {
            if (IsDisposedState()) return;
            if (!IsHandleCreated) return;

            try
            {
                if (InvokeRequired) BeginInvoke(action);
                else action();
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void RaiseCloseRequestedOnce()
        {
            if (Interlocked.Exchange(ref _closeEventRaised, 1) != 0)
                return;

            RunOnUi(() => CallCloseRequested?.Invoke(this, EventArgs.Empty));
        }

        private bool IsDisposedState() => IsDisposed || Interlocked.CompareExchange(ref _disposedFlag, 0, 0) == 1;

        private void CleanupLifetimeResources()
        {
            if (Interlocked.Exchange(ref _disposedFlag, 1) != 0)
                return;

            try { _lifetimeCts.Cancel(); } catch { }
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                StopMediaAsync(timeout.Token).GetAwaiter().GetResult();
            }
            catch { }

            if (_ownsMediaHandlers)
            {
                TryDisposeService(_videoHandler);
                TryDisposeService(_audioHandler);
            }

            if (_ownsSignalRClient)
                TryDisposeService(_signalRClient);

            _videoHandler = null;
            _audioHandler = null;
            _signalRClient = null;

            try { _lifetimeCts.Dispose(); } catch { }
            try { _actionLock.Dispose(); } catch { }
        }

        private static void TryDisposeService(object? service)
        {
            if (service == null) return;

            try
            {
                if (service is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    return;
                }
            }
            catch { }

            try
            {
                if (service is IDisposable disposable)
                    disposable.Dispose();
            }
            catch { }
        }

        private static Label BuildLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Text = text,
                ForeColor = Color.FromArgb(230, 239, 246),
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopCenter,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
        }

        private CallCircleButton BuildButton(ButtonKind kind)
        {
            int normal = ScalePx(58);
            int end = ScalePx(66);
            var button = new CallCircleButton
            {
                Size = new Size(kind == ButtonKind.End ? end : normal, kind == ButtonKind.End ? end : normal),
                ButtonKind = kind,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                IconImage = LoadIcon(kind)
            };
            return button;
        }

        private int ScalePx(int px) => (int)Math.Round(px * (DeviceDpi / 96f));

        private static Image? LoadIcon(ButtonKind kind)
        {
            var fileCandidates = kind switch
            {
                ButtonKind.Accept => new[] { "accept.png", "call_accept.png", "phone_accept.png" },
                ButtonKind.Reject => new[] { "reject.png", "call_reject.png", "phone_reject.png" },
                ButtonKind.End => new[] { "end.png", "call_end.png", "phone_end.png" },
                ButtonKind.Mic => new[] { "mic.png", "microphone.png", "mute.png" },
                ButtonKind.Camera => new[] { "camera.png", "video.png", "toggle_camera.png" },
                _ => Array.Empty<string>()
            };

            string iconsDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "calls");
            foreach (var file in fileCandidates)
            {
                string full = Path.Combine(iconsDir, file);
                if (!File.Exists(full)) continue;

                try
                {
                    using var fs = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var img = Image.FromStream(fs);
                    return new Bitmap(img);
                }
                catch
                {
                    // ignore broken icon and continue
                }
            }

            return null;
        }

        private static void EnableDoubleBufferRecursive(Control root)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            prop?.SetValue(root, true);
            foreach (Control child in root.Controls)
                EnableDoubleBufferRecursive(child);
        }
    }

    public enum CallUiState
    {
        Incoming,
        Ringing,
        InCall,
        Ended
    }

    public enum CallControlAction
    {
        Accept,
        Reject,
        End,
        ToggleMic,
        ToggleCamera,
        RemoteAccepted,
        RemoteRejected,
        RemoteEnded
    }

    public sealed class CallActionEventArgs : EventArgs
    {
        public CallControlAction Action { get; }
        public string CallId { get; }

        public CallActionEventArgs(CallControlAction action, string callId)
        {
            Action = action;
            CallId = callId;
        }
    }

    internal enum ButtonKind
    {
        Accept,
        Reject,
        End,
        Mic,
        Camera
    }

    internal sealed class CallCircleButton : Control
    {
        public ButtonKind ButtonKind { get; set; }
        public bool IsToggled { get; set; }
        public Image? IconImage { get; set; }

        private bool _hover;
        private bool _pressed;
        private float _hoverProgress;
        private float _hoverTarget;
        private readonly System.Windows.Forms.Timer _hoverTimer;

        public CallCircleButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;

            _hoverTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _hoverTimer.Tick += (_, __) =>
            {
                const float step = 0.14f;
                if (Math.Abs(_hoverProgress - _hoverTarget) < 0.01f)
                {
                    _hoverProgress = _hoverTarget;
                    _hoverTimer.Stop();
                    Invalidate();
                    return;
                }

                _hoverProgress += _hoverProgress < _hoverTarget ? step : -step;
                _hoverProgress = Math.Clamp(_hoverProgress, 0f, 1f);
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            _hoverTarget = 1f;
            _hoverTimer.Start();
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            _hoverTarget = 0f;
            _hoverTimer.Start();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = false;
                Invalidate();
                if (ClientRectangle.Contains(e.Location))
                    OnClick(EventArgs.Empty);
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);
            Color baseColor = ResolveBaseColor();
            Color hoverColor = ControlPaint.Light(baseColor, 0.11f);
            Color bg = Lerp(baseColor, hoverColor, _hoverProgress);
            if (_pressed) bg = ControlPaint.Dark(bg, 0.08f);

            float scale = _pressed ? 0.95f : 1f;
            var state = g.Save();
            g.TranslateTransform(Width / 2f, Height / 2f);
            g.ScaleTransform(scale, scale);
            g.TranslateTransform(-Width / 2f, -Height / 2f);

            using (var br = new SolidBrush(bg))
                g.FillEllipse(br, r);

            using (var edge = new Pen(Color.FromArgb(95, 255, 255, 255), 1.1f))
                g.DrawEllipse(edge, r);

            DrawIcon(g, r);
            g.Restore(state);
        }

        private Color ResolveBaseColor()
        {
            return ButtonKind switch
            {
                ButtonKind.Accept => Color.FromArgb(40, 170, 95),
                ButtonKind.Reject => Color.FromArgb(95, 100, 110),
                ButtonKind.End => Color.FromArgb(225, 70, 75),
                ButtonKind.Mic => IsToggled ? Color.FromArgb(225, 70, 75) : Color.FromArgb(49, 61, 77),
                ButtonKind.Camera => IsToggled ? Color.FromArgb(225, 70, 75) : Color.FromArgb(49, 61, 77),
                _ => Color.FromArgb(49, 61, 77)
            };
        }

        private void DrawIcon(Graphics g, Rectangle r)
        {
            int cx = r.Left + r.Width / 2;
            int cy = r.Top + r.Height / 2;

            if (IconImage != null)
            {
                int iconSize = Math.Min(r.Width, r.Height) / 2;
                var iconRect = new Rectangle(cx - iconSize / 2, cy - iconSize / 2, iconSize, iconSize);
                g.DrawImage(IconImage, iconRect);
                return;
            }

            using var pen = new Pen(Color.White, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var penThick = new Pen(Color.White, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

            switch (ButtonKind)
            {
                case ButtonKind.Accept:
                    g.DrawArc(penThick, cx - 12, cy - 9, 24, 18, 200, 140);
                    g.DrawLine(penThick, cx - 12, cy + 3, cx - 7, cy - 1);
                    g.DrawLine(penThick, cx + 12, cy + 3, cx + 7, cy - 1);
                    break;

                case ButtonKind.Reject:
                case ButtonKind.End:
                    g.DrawArc(penThick, cx - 13, cy - 8, 26, 18, 200, 140);
                    g.DrawLine(penThick, cx - 11, cy + 5, cx - 5, cy + 1);
                    g.DrawLine(penThick, cx + 11, cy + 5, cx + 5, cy + 1);
                    break;

                case ButtonKind.Mic:
                    DrawRoundedRect(g, pen, cx - 6, cy - 12, 12, 16, 6);
                    g.DrawLine(pen, cx, cy + 4, cx, cy + 12);
                    g.DrawArc(pen, cx - 10, cy + 1, 20, 14, 15, 150);
                    g.DrawLine(pen, cx - 6, cy + 12, cx + 6, cy + 12);
                    if (IsToggled)
                        g.DrawLine(penThick, cx - 14, cy + 12, cx + 14, cy - 12);
                    break;

                case ButtonKind.Camera:
                    DrawRoundedRect(g, pen, cx - 14, cy - 8, 18, 14, 4);
                    g.DrawEllipse(pen, cx - 9, cy - 4, 8, 8);
                    g.FillPolygon(Brushes.White, new[]
                    {
                        new PointF(cx + 4,  cy - 4),
                        new PointF(cx + 13, cy - 8),
                        new PointF(cx + 13, cy + 7),
                        new PointF(cx + 4,  cy + 3)
                    });
                    if (IsToggled)
                        g.DrawLine(penThick, cx - 14, cy + 12, cx + 14, cy - 12);
                    break;
            }
        }

        private static void DrawRoundedRect(Graphics g, Pen p, int x, int y, int w, int h, int radius)
        {
            using var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            g.DrawPath(p, path);
        }

        private static Color Lerp(Color from, Color to, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            int a = from.A + (int)((to.A - from.A) * t);
            int r = from.R + (int)((to.R - from.R) * t);
            int g = from.G + (int)((to.G - from.G) * t);
            int b = from.B + (int)((to.B - from.B) * t);
            return Color.FromArgb(a, r, g, b);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hoverTimer.Stop();
                _hoverTimer.Dispose();
                IconImage?.Dispose();
                IconImage = null;
            }
            base.Dispose(disposing);
        }
    }
}
