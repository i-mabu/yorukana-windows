using System.Runtime.InteropServices;
using System.Text;

namespace YoruKanaWin;

// ===== よるかな for Windows エントリポイント =====
// トレイ常駐の入力コンパニオン。グローバルホットキー (既定 Ctrl+Alt+J) で
// 入力ウィンドウを開き、確定したテキストを直前のアプリへ送り込む。
// TSF のシステム IME ではない (方式の詳細は README を参照)。
internal static class Program
{
    internal const string Version = "1.1.1";

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    private const int AttachParentProcess = -1;
    private static Mutex? _instanceMutex;
    private static bool _mutexOwned;

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "--version" || args[0] == "version"))
        {
            AttachConsole(AttachParentProcess);
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("よるかな for Windows " + Version);
            return;
        }

        // 二重起動防止: WaitOne 方式で所有権を取る。
        // 前プロセスが異常終了して Mutex が放棄されていた場合は
        // AbandonedMutexException が出るが、その時点で所有権は得られている。
        _instanceMutex = new Mutex(initiallyOwned: false, "YoruKana-Windows.SingleInstance", out bool createdNew);
        try
        {
            _mutexOwned = _instanceMutex.WaitOne(0, exitContext: false);
        }
        catch (AbandonedMutexException)
        {
            _mutexOwned = true; // 放棄された Mutex の所有権を引き継いだ
        }
        if (!_mutexOwned)
        {
            _instanceMutex.Dispose();
            _instanceMutex = null;
            return; // 既に起動中
        }
        _ = createdNew;

        // グローバル例外ハンドラ: ログ記録＋通知し、UIスレッド例外では継続する
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            ErrorLogger.Log("ThreadException", e.Exception);
            TrayAppContext.Current?.ShowErrorBalloon(
                "エラーが発生しましたが、動作を継続します。詳細は error.log を確認してください。");
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            ErrorLogger.Log("UnhandledException", e.ExceptionObject as Exception);
            TrayAppContext.Current?.ShowErrorBalloon(
                "予期しないエラーが発生しました。詳細は error.log を確認してください。");
        };

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try { Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); } catch (Exception) { /* 既定で続行 */ }
        try
        {
            Application.Run(new TrayAppContext());
        }
        finally
        {
            if (_mutexOwned)
            {
                try { _instanceMutex?.ReleaseMutex(); } catch (Exception) { /* 二重解放等は無視 */ }
            }
            _instanceMutex?.Dispose();
            _instanceMutex = null;
        }
    }
}

// ===== エラーログ =====
// %AppData%\YoruKana\error.log へ日時付きで追記する。記録自体の失敗は無視する。
internal static class ErrorLogger
{
    internal static void Log(string kind, Exception? ex)
    {
        try
        {
            string dir = LearningFileStore.AppDataDir();
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "error.log");
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {kind}: {ex}\n";
            File.AppendAllText(path, line, Encoding.UTF8);
        }
        catch (Exception) { /* ログ失敗で本体を落とさない */ }
    }
}

// ===== トレイアプリケーション本体 =====
internal sealed class TrayAppContext : ApplicationContext
{
    internal static TrayAppContext? Current { get; private set; }

    private readonly NotifyIcon _tray = new();
    private readonly JapaneseDictionary _dictionary = new();
    private readonly LearningFileStore _learning = new();
    private readonly UserDictionaryFileStore _userDict = new();
    private readonly ConversionEngine _engine;
    private readonly InputForm _inputForm;
    private readonly HotkeyWindow _hotkey = new();
    private IntPtr _lastTarget = IntPtr.Zero;

    private Icon? _appIcon;
    private IntPtr _appIconHandle = IntPtr.Zero;

    private const int HotkeyId = 1;

    public TrayAppContext()
    {
        Current = this;
        _engine = new ConversionEngine(_dictionary, _learning, _userDict.Dictionary);
        _inputForm = new InputForm(_engine, _learning);

        var menu = new ContextMenuStrip();
        menu.Items.Add("入力ウィンドウを開く (Ctrl+Alt+J)", null, (_, _) => ToggleInputWindow());
        menu.Items.Add("ユーザー辞書を編集...", null, (_, _) => EditUserDictionary());
        menu.Items.Add("学習リセット", null, (_, _) =>
        {
            _learning.Clear();
            _tray.ShowBalloonTip(2000, "よるかな", "学習データをリセットしました", ToolTipIcon.Info);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => ExitThread());

        _appIcon = CreateAppIcon(out _appIconHandle);
        _tray.Icon = _appIcon;
        _tray.Text = "よるかな for Windows";
        _tray.ContextMenuStrip = menu;
        _tray.Visible = true;
        _tray.DoubleClick += (_, _) => ToggleInputWindow();

        _hotkey.HotkeyPressed += ToggleInputWindow;
        bool ok = NativeMethods.RegisterHotKey(_hotkey.Handle, HotkeyId,
            NativeMethods.ModControl | NativeMethods.ModAlt | NativeMethods.ModNoRepeat,
            NativeMethods.VkJ);
        if (!ok)
        {
            _tray.ShowBalloonTip(4000, "よるかな",
                "ホットキー Ctrl+Alt+J を登録できませんでした。他のアプリと競合している可能性があります。トレイのメニューから開いてください。",
                ToolTipIcon.Warning);
        }
    }

    // グローバル例外ハンドラから呼ばれる通知
    internal void ShowErrorBalloon(string message)
    {
        try { _tray.ShowBalloonTip(4000, "よるかな", message, ToolTipIcon.Error); }
        catch (Exception) { /* 通知失敗は無視 */ }
    }

    private void EditUserDictionary()
    {
        using var form = new UserDictForm(_userDict);
        form.ShowDialog();
        _inputForm.RefreshCandidates(); // 編集結果を候補表示へ反映する
    }

    private void ToggleInputWindow()
    {
        if (_inputForm.IsSending) return; // 送信中は開閉しない
        if (_inputForm.Visible)
        {
            _inputForm.Hide();
            return;
        }
        IntPtr fg = NativeMethods.GetForegroundWindow();
        if (fg != IntPtr.Zero && fg != _inputForm.Handle) _lastTarget = fg;
        _inputForm.OpenFor(_lastTarget);
    }

    protected override void ExitThreadCore()
    {
        NativeMethods.UnregisterHotKey(_hotkey.Handle, HotkeyId);
        _hotkey.DestroyHandle();
        _tray.Visible = false;
        _tray.ContextMenuStrip?.Dispose();
        _tray.Dispose();
        _inputForm.Dispose();
        // アイコン: Icon.FromHandle はハンドルを所有しないため、明示的に破棄する。
        // Bitmap は GetHicon の時点で役目を終えている (生成側で破棄済み) ので、
        // ここでは HICON の DestroyIcon を正しい順序で行う。
        if (_appIconHandle != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_appIconHandle);
            _appIconHandle = IntPtr.Zero;
        }
        _appIcon?.Dispose();
        _appIcon = null;
        if (Current == this) Current = null;
        base.ExitThreadCore();
    }

    // 夜空色の丸に「よ」を描いた簡易アイコン (失敗時は既定アイコン)
    private static Icon CreateAppIcon(out IntPtr hIcon)
    {
        hIcon = IntPtr.Zero;
        try
        {
            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(13, 17, 35));
                using var font = new Font("Yu Gothic UI", 18f, FontStyle.Bold);
                using var brush = new SolidBrush(Color.FromArgb(232, 236, 248));
                g.DrawString("よ", font, brush, 2, 2);
            }
            // GetHicon で得た HICON は Bitmap と独立しているため、
            // Bitmap はここで破棄してよく、HICON は終了時に DestroyIcon する。
            hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }
        catch (Exception)
        {
            hIcon = IntPtr.Zero;
            return SystemIcons.Application;
        }
    }
}

// ===== ホットキー受信用の不可視ウィンドウ =====
internal sealed class HotkeyWindow : NativeWindow
{
    internal event Action? HotkeyPressed;

    internal HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey)
        {
            HotkeyPressed?.Invoke();
        }
        base.WndProc(ref m);
    }
}
