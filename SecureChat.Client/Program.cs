using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using SecureChat.Client.Components.Call;
using SecureChat.Client.Forms.Call;

using SecureChat.Client.Forms.Settings;
using SecureChat.Client.Models;

namespace SecureChat.Client
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new frmUiTestLauncher());
        }
    }

    internal sealed class frmUiTestLauncher : Form
    {
        public frmUiTestLauncher()
        {
            Text = "SecureChat - UI Test Launcher";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 330);
            BackColor = Color.FromArgb(0x17, 0x21, 0x2E);
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);

            var title = new Label
            {
                Text = "Run quick tests for Video Call features",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold, GraphicsUnit.Point),
                AutoSize = true,
                Location = new Point(24, 24)
            };

            var sub = new Label
            {
                Text = "Choose one test screen below:",
                ForeColor = Color.FromArgb(190, 210, 230),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
                AutoSize = true,
                Location = new Point(26, 60)
            };

            var panel = new FlowLayoutPanel
            {
                Location = new Point(24, 92),
                Size = new Size(512, 206),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            panel.Controls.Add(BuildLauncherButton("Test Video Call Form (frmVideoCall)", (_, __) =>
            {
                using var f = new frmVideoCall("Hoàng Minh Hiếu");
                f.ShowDialog(this);
            }));

            panel.Controls.Add(BuildLauncherButton("Test Call Controls (ucCallControls)", (_, __) =>
            {
                using var f = new frmCallControlsTestHarness();
                f.ShowDialog(this);
            }));

            panel.Controls.Add(BuildLauncherButton("Test Speakers & Camera Settings", (_, __) =>
            {
                using var f = new frmSpeakersCamera();
                f.ShowDialog(this);
            }));

            panel.Controls.Add(BuildLauncherButton("Test Full Settings Entry", (_, __) =>
            {
                var fakeProfile = new ProfileModel
                {
                    FullName = "Hoàng Minh Hiếu",
                    PhoneNumber = "0903187536",
                    Username = "minhhieu_dev1",
                    Birthday = new DateTime(2003, 9, 15),
                    StatusText = "online"
                };

                using var f = new frmSettings(fakeProfile);
                f.ShowDialog(this);
            }));

            Controls.Add(title);
            Controls.Add(sub);
            Controls.Add(panel);
        }

        private static Button BuildLauncherButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = 492,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x2C, 0x3B, 0x4F),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(14, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
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

            var commandHost = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.None,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 8, 10, 8),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(0x1E, 0x2A, 0x3A),
                Margin = Padding.Empty
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

            commandHost.Controls.Add(topBar);

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
            Controls.Add(commandHost);

            void LayoutCommandBar()
            {
                topBar.Location = new Point(
                    Math.Max(0, (commandHost.ClientSize.Width - topBar.Width) / 2),
                    Math.Max(0, (commandHost.ClientSize.Height - topBar.Height) / 2));
            }

            commandHost.Resize += (_, __) => LayoutCommandBar();
            Resize += (_, __) => LayoutCommandBar();
            Shown += (_, __) => LayoutCommandBar();

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

            Application.Run(new frmSettings(fakeProfile));
            */
            Application.Run(new frmTwoFA());
            // Application.Run(new TwoFAForm());
            // Application.Run(new MainForm());
            // Application.Run(new frmLoginRegister());
            // Application.Run(new ForgotPasswordForm());
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
