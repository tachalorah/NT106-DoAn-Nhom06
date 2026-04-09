using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SecureChat.Client.Components.Call;

namespace SecureChat.Client
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new frmCallControlsTestHarness());
        }
    }

    internal sealed class frmCallControlsTestHarness : Form
    {
        private readonly ucCallControls _controls;
        private readonly ListBox _log;
        private readonly FakeSignalRClient _signalR;
        private readonly FakeVideoHandler _video;
        private readonly FakeAudioHandler _audio;

        public frmCallControlsTestHarness()
        {
            Text = "Call Controls Test Harness";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(980, 620);
            BackColor = Color.FromArgb(0x17, 0x21, 0x2E);
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);

            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(10, 8, 10, 8),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(0x1E, 0x2A, 0x3A)
            };

            var btnIncoming = BuildActionButton("State: Incoming", (_, __) => _controls.SetUiState(CallUiState.Incoming));
            var btnInCall = BuildActionButton("State: InCall", (_, __) => _controls.SetUiState(CallUiState.InCall));
            var btnEnded = BuildActionButton("State: Ended", (_, __) => _controls.SetUiState(CallUiState.Ended));
            var btnRemoteAccepted = BuildActionButton("Remote: ACCEPTED", async (_, __) => await ApplyRemoteAsync("CALL_ACCEPTED"));
            var btnRemoteRejected = BuildActionButton("Remote: REJECTED", async (_, __) => await ApplyRemoteAsync("CALL_REJECTED"));
            var btnRemoteEnded = BuildActionButton("Remote: ENDED", async (_, __) => await ApplyRemoteAsync("CALL_ENDED"));

            topBar.Controls.AddRange(new Control[]
            {
                btnIncoming, btnInCall, btnEnded, btnRemoteAccepted, btnRemoteRejected, btnRemoteEnded
            });

            _log = new ListBox
            {
                Dock = DockStyle.Right,
                Width = 360,
                Font = new Font("Consolas", 10f, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(0x12, 0x1B, 0x28),
                ForeColor = Color.FromArgb(230, 239, 246),
                BorderStyle = BorderStyle.None
            };

            var centerHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(0x17, 0x21, 0x2E)
            };

            _controls = new ucCallControls
            {
                Dock = DockStyle.Bottom,
                Height = 132,
                BackColor = Color.Transparent
            };

            _signalR = new FakeSignalRClient(AppendLog);
            _video = new FakeVideoHandler(AppendLog);
            _audio = new FakeAudioHandler(AppendLog);

            _controls.ConfigureServices(_signalR, _video, _audio, callId: "call-test-001");
            _controls.SetUiState(CallUiState.Incoming);

            _controls.ActionExecuted += (_, e) => AppendLog($"UI Action => {e.Action} | CallId={e.CallId}");
            _controls.OperationFailed += (_, ex) => AppendLog($"ERROR => {ex.Message}");
            _controls.CallCloseRequested += (_, __) => AppendLog("CallCloseRequested fired");

            _signalR.SignalSent += (_, payload) => AppendLog($"Signal Sent => {payload}");

            centerHost.Controls.Add(_controls);

            Controls.Add(centerHost);
            Controls.Add(_log);
            Controls.Add(topBar);

            AppendLog("Test harness ready.");
            AppendLog("Click Accept/Reject/End/Mute/Camera on ucCallControls below.");
        }

        private Button BuildActionButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x2C, 0x3B, 0x4F),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(10, 6, 10, 6),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private async Task ApplyRemoteAsync(string signal)
        {
            await _controls.ApplyRemoteSignalAsync(signal);
        }

        private void AppendLog(string message)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), message);
                return;
            }

            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _log.Items.Insert(0, line);
            if (_log.Items.Count > 300)
                _log.Items.RemoveAt(_log.Items.Count - 1);
        }
    }

    internal sealed class FakeSignalRClient
    {
        private readonly Action<string> _logger;
        public bool IsConnected { get; set; } = true;

        public event EventHandler<string>? SignalSent;

        public FakeSignalRClient(Action<string> logger)
        {
            _logger = logger;
        }

        public Task SendCallSignalAsync(string callId, string signal, CancellationToken cancellationToken = default)
        {
            string payload = $"callId={callId}, signal={signal}";
            _logger($"FakeSignalR.SendCallSignalAsync({payload})");
            SignalSent?.Invoke(this, payload);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeVideoHandler
    {
        private readonly Action<string> _logger;

        public FakeVideoHandler(Action<string> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger("FakeVideoHandler.StartAsync");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger("FakeVideoHandler.StopAsync");
            return Task.CompletedTask;
        }

        public Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        {
            _logger($"FakeVideoHandler.SetEnabledAsync(enabled={enabled})");
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeAudioHandler
    {
        private readonly Action<string> _logger;

        public FakeAudioHandler(Action<string> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger("FakeAudioHandler.StartAsync");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger("FakeAudioHandler.StopAsync");
            return Task.CompletedTask;
        }

        public Task SetMutedAsync(bool muted, CancellationToken cancellationToken = default)
        {
            _logger($"FakeAudioHandler.SetMutedAsync(muted={muted})");
            return Task.CompletedTask;
        }
    }
}
