using System.Runtime.InteropServices;

namespace YoruKanaWin;

// ===== 送信結果 =====
// 呼び出し側 (InputForm) が失敗理由に応じてユーザーへ通知できるよう、
// 単なる成功/失敗ではなく理由を区別して返す。
internal enum TextSendResult
{
    Success,             // 送信できた (貼り付けまたは SendInput)
    TargetGone,          // 送信先ウィンドウが既に破棄されていたため中止
    ForegroundMismatch,  // 送信先を前面化できなかった/別ウィンドウが前面のため中止
    Failed,              // 貼り付け・SendInput のどちらも失敗
}

// ===== テキスト送信 (確定した文字列を対象アプリへ送り込む) =====
// 基本戦略: 対象ウィンドウをフォアグラウンドに戻し、クリップボード経由で
// Ctrl+V 貼り付けする (元のクリップボード内容は保存して復元する)。
// クリップボードが使えない場合は SendInput の Unicode 入力にフォールバックする。
// 送信先の検証: IsWindow で生存確認し、SetForegroundWindow の結果と
// 送信直前の GetForegroundWindow() が対象と一致することを確認する。
// 一致しない場合は誤送信防止のため貼り付けを中止する。
// 注意: 管理者権限で動いているアプリには、通常権限からは送信できない場合がある
// (Windows の UIPI)。README の制限事項を参照。
internal static class TextSender
{
    // target を前面に戻してから text を送信する。UIスレッドから呼ぶこと。
    internal static async Task<TextSendResult> SendAsync(IntPtr target, string text)
    {
        if (string.IsNullOrEmpty(text)) return TextSendResult.Success;

        if (target != IntPtr.Zero)
        {
            // 送信先が既に破棄されていたら誤送信を防ぐため中止する
            if (!NativeMethods.IsWindow(target))
            {
                return TextSendResult.TargetGone;
            }
            NativeMethods.ShowWindow(target, NativeMethods.SwRestore);
            bool fgOk = NativeMethods.SetForegroundWindow(target);
            // フォーカス遷移が落ち着くのを少し待つ
            await Task.Delay(120);
            // 送信直前の事後確認: 前面が対象でなければ貼り付けを中止する
            if (!fgOk || NativeMethods.GetForegroundWindow() != target)
            {
                return TextSendResult.ForegroundMismatch;
            }
        }
        else
        {
            await Task.Delay(120);
        }

        if (await TryPasteAsync(text))
        {
            return TextSendResult.Success;
        }
        return SendUnicode(text) ? TextSendResult.Success : TextSendResult.Failed;
    }

    // クリップボード貼り付け。成功したら true。
    // 元のクリップボード内容は、どの失敗パスでも finally で書き戻す。
    private static async Task<bool> TryPasteAsync(string text)
    {
        IDataObject? saved = null;
        bool haveSaved = false;
        bool clipboardChanged = false;
        try
        {
            try { saved = Clipboard.GetDataObject(); haveSaved = true; }
            catch (Exception) { saved = null; }

            Clipboard.SetText(text);
            clipboardChanged = true;

            SendKeys.SendWait("^v");

            // 貼り付けが実際に処理されるまで待つ (復元は finally で行う)
            await Task.Delay(250);
            return true;
        }
        catch (Exception)
        {
            return false; // クリップボードが他アプリにロックされている等
        }
        finally
        {
            // 自分が書き換えた場合のみ復元する (書き換え前の失敗で元内容を壊さない)
            if (clipboardChanged && haveSaved)
            {
                try
                {
                    if (saved != null) Clipboard.SetDataObject(saved, copy: true);
                    else Clipboard.Clear();
                }
                catch (Exception) { /* 復元失敗は無視 */ }
            }
        }
    }

    // SendInput による Unicode 直接入力 (1文字ずつ keydown/keyup)。
    // SendInput の戻り値 (実際にキューへ入った件数) を検証し、
    // 全件送れた場合のみ true を返す。
    private static bool SendUnicode(string text)
    {
        var inputs = new List<NativeMethods.Input>();
        foreach (char c in text)
        {
            inputs.Add(MakeKey(c, keyUp: false));
            inputs.Add(MakeKey(c, keyUp: true));
        }
        if (inputs.Count == 0) return true;
        uint sent = NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(),
            Marshal.SizeOf<NativeMethods.Input>());
        return sent == (uint)inputs.Count;
    }

    private static NativeMethods.Input MakeKey(char c, bool keyUp)
    {
        return new NativeMethods.Input
        {
            Type = NativeMethods.InputKeyboard,
            U = new NativeMethods.InputUnion
            {
                Ki = new NativeMethods.KeybdInput
                {
                    WVk = 0,
                    WScan = c,
                    DwFlags = NativeMethods.KeyeventfUnicode | (keyUp ? NativeMethods.KeyeventfKeyup : 0),
                    Time = 0,
                    DwExtraInfo = IntPtr.Zero,
                },
            },
        };
    }
}
