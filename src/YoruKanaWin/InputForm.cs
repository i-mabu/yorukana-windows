namespace YoruKanaWin;

// ===== 入力ウィンドウ (入力コンパニオン) =====
// ホットキーで呼び出され、ローマ字入力→変換→Enterで対象アプリへ送信する小さな窓。
// 操作モデル:
//   - 文字キー: ローマ字を composing に追加 (英数モードでは直接バッファへ)
//   - Space: 変換開始／候補送り (英数モードでは空白を入力)
//   - Enter: 現在の区切りを確定し、蓄えた全文を対象アプリへ送信して閉じる
//   - 候補クリック: その候補で現在の区切りを確定し、入力を続ける (送信はしない)
//   - Backspace: 変換中なら変換を解除、それ以外は1文字削除
//   - Esc: すべてキャンセルして閉じる (送信しない)
//   - Alt+A または左上のモード表示クリック: あ (日本語) / A (英数) 切替
// カーソル移動の概念はない (←→キーは無効)。README の制限事項を参照。
public sealed class InputForm : Form
{
    // 夜テーマ
    private static readonly Color Bg = Color.FromArgb(13, 17, 35);
    private static readonly Color Panel = Color.FromArgb(26, 33, 64);
    private static readonly Color CandBg = Color.FromArgb(35, 43, 82);
    private static readonly Color CandSelected = Color.FromArgb(61, 90, 254);
    private static readonly Color Fg = Color.FromArgb(232, 236, 248);
    private static readonly Color SubFg = Color.FromArgb(154, 165, 199);

    private readonly ConversionEngine _engine;
    private readonly ILearningStore _learning;

    private readonly Label _modeLabel = new();
    private readonly Label _displayLabel = new();
    private readonly FlowLayoutPanel _candidatePanel = new();

    private sealed class CandidateItem
    {
        public required string Text;
        public required string Reading; // 学習記録用の読み
    }

    // 候補ラベルで使い回すフォント (RefreshUi のたびに生成すると GDI ハンドルが漏れる)
    private readonly Font _candidateFont = new Font("Yu Gothic UI", 12f);

    private bool _englishMode;
    private bool _isSending;       // 送信処理中は再オープン/再送信を抑止する
    public bool IsSending => _isSending;
    private string _prefix = "";   // 確定済みテキスト (このセッションで蓄える)
    private string _romaji = "";   // 現在の区切りのローマ字バッファ
    private string _direct = "";   // 英数モードのバッファ
    private List<CandidateItem> _candidates = new();
    private int _selected = -1;    // 変換中候補インデックス (-1 = 未変換)
    private IntPtr _target = IntPtr.Zero;

    public InputForm(ConversionEngine engine, ILearningStore learning)
    {
        _engine = engine;
        _learning = learning;

        Text = "よるかな";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(600, 168);
        BackColor = Bg;
        ForeColor = Fg;
        KeyPreview = true;
        Font = new Font("Yu Gothic UI", 11f);

        _modeLabel.Text = "あ";
        _modeLabel.Font = new Font("Yu Gothic UI", 14f, FontStyle.Bold);
        _modeLabel.ForeColor = Fg;
        _modeLabel.BackColor = Panel;
        _modeLabel.TextAlign = ContentAlignment.MiddleCenter;
        _modeLabel.SetBounds(10, 10, 44, 40);
        _modeLabel.Cursor = Cursors.Hand;
        _modeLabel.Click += (_, _) => ToggleMode();
        Controls.Add(_modeLabel);

        var hint = new Label
        {
            Text = "Space: 変換　Enter: 送信　Esc: 取消　Alt+A: あ/A切替",
            ForeColor = SubFg,
            Font = new Font("Yu Gothic UI", 9f),
            AutoSize = true,
        };
        hint.SetBounds(64, 20, 500, 20);
        Controls.Add(hint);

        _displayLabel.Font = new Font("Yu Gothic UI", 16f);
        _displayLabel.ForeColor = Fg;
        _displayLabel.BackColor = Panel;
        _displayLabel.TextAlign = ContentAlignment.MiddleLeft;
        _displayLabel.SetBounds(10, 58, 580, 44);
        Controls.Add(_displayLabel);

        _candidatePanel.FlowDirection = FlowDirection.LeftToRight;
        _candidatePanel.WrapContents = true;
        _candidatePanel.BackColor = Bg;
        _candidatePanel.SetBounds(10, 110, 580, 50);
        Controls.Add(_candidatePanel);

        KeyDown += OnKeyDown;
        KeyPress += OnKeyPress;
    }

    // 対象ウィンドウを指定して開く (状態はリセットされる)
    public void OpenFor(IntPtr target)
    {
        if (_isSending) return; // 送信中は再オープンしない
        _target = target;
        _prefix = "";
        _romaji = "";
        _direct = "";
        _selected = -1;
        RefreshUi();
        Show();
        Activate();
    }

    private void ToggleMode()
    {
        _englishMode = !_englishMode;
        _selected = -1;
        RefreshUi();
    }

    // ----- 状態から表示を再構築 -----
    private string CurrentReading() => RomajiConverter.ConvertFully(_romaji);

    private string SegmentDisplay()
    {
        if (_englishMode) return _direct;
        if (_selected >= 0 && _selected < _candidates.Count) return _candidates[_selected].Text;
        var r = RomajiConverter.Convert(_romaji);
        return r.Kana + r.Pending; // 未確定ローマ字はそのまま末尾に表示
    }

    private void RefreshUi()
    {
        _modeLabel.Text = _englishMode ? "A" : "あ";
        _displayLabel.Text = _prefix + SegmentDisplay();
        RebuildCandidates();
    }

    private void RebuildCandidates()
    {
        _candidatePanel.SuspendLayout();
        // Controls.Clear() はコントロールを破棄しないため、先に Dispose する
        // (しないと候補を更新するたびにラベルのハンドルが漏れる)
        while (_candidatePanel.Controls.Count > 0)
        {
            Control c = _candidatePanel.Controls[0];
            _candidatePanel.Controls.RemoveAt(0);
            c.Dispose();
        }
        _candidates = BuildCandidates();
        if (_selected >= _candidates.Count) _selected = -1;
        for (int i = 0; i < _candidates.Count; i++)
        {
            int idx = i;
            var item = _candidates[i];
            var label = new Label
            {
                Text = item.Text,
                AutoSize = true,
                ForeColor = Fg,
                BackColor = idx == _selected ? CandSelected : CandBg,
                Font = _candidateFont, // 共有フォントを使い回す (ラベルごとに生成しない)
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 0, 6, 6),
                Cursor = Cursors.Hand,
            };
            label.Click += (_, _) => FixSegment(idx, send: false);
            _candidatePanel.Controls.Add(label);
        }
        _candidatePanel.ResumeLayout();
    }

    // 外部 (ユーザー辞書の編集後など) から表示を更新する
    public void RefreshCandidates()
    {
        if (!_isSending) RefreshUi();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _candidateFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private List<CandidateItem> BuildCandidates()
    {
        var list = new List<CandidateItem>();
        if (_englishMode || _romaji.Length == 0) return list;
        string reading = CurrentReading();
        var seen = new HashSet<string>();
        foreach (string c in _engine.Candidates(reading, _romaji))
        {
            if (seen.Add(c)) list.Add(new CandidateItem { Text = c, Reading = reading });
        }
        // 前方一致の予測候補も続けて並べる (学習記録には各候補の読みを使う)
        foreach (string[] e in _engine.PredictEntries(reading, 6))
        {
            if (seen.Add(e[1])) list.Add(new CandidateItem { Text = e[1], Reading = e[0] });
        }
        return list;
    }

    // ----- 確定・送信 -----
    // 現在の区切りを候補 (または読みそのまま) で確定して _prefix へ移す。
    // send=true なら全文を対象アプリへ送信して閉じる。
    private async void FixSegment(int candidateIndex, bool send)
    {
        if (send && _isSending) return; // 送信の直列化 (二重送信防止)
        string fixedText;
        if (_englishMode)
        {
            fixedText = _direct;
            _direct = "";
        }
        else if (_romaji.Length == 0)
        {
            fixedText = "";
        }
        else if (candidateIndex >= 0 && candidateIndex < _candidates.Count)
        {
            CandidateItem item = _candidates[candidateIndex];
            fixedText = item.Text;
            // ひらがな原文・ローマ字原文以外の確定を学習する
            if (item.Text != item.Reading && item.Text != _romaji)
            {
                _learning.RecordSelection(item.Reading, item.Text);
            }
        }
        else
        {
            fixedText = CurrentReading();
        }

        _prefix += fixedText;
        _romaji = "";
        _selected = -1;

        if (send)
        {
            string toSend = _prefix;
            _prefix = "";
            IntPtr target = _target;
            _isSending = true;
            Hide();
            try
            {
                TextSendResult result = await TextSender.SendAsync(target, toSend);
                if (result != TextSendResult.Success && toSend.Length > 0)
                {
                    NotifySendFailure(result, toSend);
                }
            }
            finally
            {
                _isSending = false;
            }
        }
        else
        {
            RefreshUi();
        }
    }

    // 送信できなかった場合の通知。文章は失われないようクリップボードへ残す。
    private static void NotifySendFailure(TextSendResult result, string text)
    {
        string reason = result switch
        {
            TextSendResult.TargetGone => "送信先のウィンドウが閉じられていました",
            TextSendResult.ForegroundMismatch => "送信先のウィンドウを前面にできませんでした",
            _ => "貼り付けと直接入力のどちらも失敗しました",
        };
        try { Clipboard.SetText(text); } catch (Exception) { /* コピー失敗時も通知は出す */ }
        MessageBox.Show(
            $"送信できませんでした（{reason}）。\n文章はクリップボードにコピーしました。貼り付け先で Ctrl+V してください。",
            "よるかな", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void CancelInput()
    {
        _prefix = "";
        _romaji = "";
        _direct = "";
        _selected = -1;
        Hide();
    }

    // ----- キー処理 -----
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Alt && e.KeyCode == Keys.A)
        {
            ToggleMode();
            e.SuppressKeyPress = true;
            e.Handled = true;
            return;
        }
        switch (e.KeyCode)
        {
            case Keys.Escape:
                CancelInput();
                e.SuppressKeyPress = true;
                e.Handled = true;
                break;
            case Keys.Enter:
                FixSegment(_selected, send: true);
                e.SuppressKeyPress = true;
                e.Handled = true;
                break;
            case Keys.Space:
                if (_englishMode)
                {
                    _direct += " ";
                    RefreshUi();
                }
                else if (_romaji.Length > 0)
                {
                    // 変換開始／候補送り
                    _selected = _candidates.Count == 0 ? -1 : (_selected + 1) % _candidates.Count;
                    RefreshUi();
                }
                e.SuppressKeyPress = true;
                e.Handled = true;
                break;
            case Keys.Back:
                OnBackspace();
                e.SuppressKeyPress = true;
                e.Handled = true;
                break;
        }
    }

    private void OnBackspace()
    {
        if (_selected >= 0)
        {
            _selected = -1; // 変換を解除して読み表示に戻る
        }
        else if (_englishMode)
        {
            if (_direct.Length > 0) _direct = _direct.Substring(0, _direct.Length - 1);
            else if (_prefix.Length > 0) _prefix = _prefix.Substring(0, _prefix.Length - 1);
        }
        else if (_romaji.Length > 0)
        {
            _romaji = _romaji.Substring(0, _romaji.Length - 1);
        }
        else if (_prefix.Length > 0)
        {
            _prefix = _prefix.Substring(0, _prefix.Length - 1);
        }
        RefreshUi();
    }

    private void OnKeyPress(object? sender, KeyPressEventArgs e)
    {
        char c = e.KeyChar;
        if (c < ' ' || c == '\x7f') { return; } // 制御文字は KeyDown 側で処理済み

        if (_englishMode)
        {
            _direct += c;
            RefreshUi();
            e.Handled = true;
            return;
        }

        // 日本語 (ローマ字) モード
        if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '-' || c == '\'')
        {
            _romaji += char.ToLowerInvariant(c);
            _selected = -1;
            RefreshUi();
            e.Handled = true;
            return;
        }
        if (c == ',')
        {
            _prefix += SegmentAsFixed() + "、";
            _romaji = ""; _selected = -1;
            RefreshUi();
            e.Handled = true;
            return;
        }
        if (c == '.')
        {
            _prefix += SegmentAsFixed() + "。";
            _romaji = ""; _selected = -1;
            RefreshUi();
            e.Handled = true;
            return;
        }
        // その他の文字 (数字・記号) は現在の読みを確定してからそのまま蓄える
        _prefix += SegmentAsFixed() + c;
        _romaji = ""; _selected = -1;
        RefreshUi();
        e.Handled = true;
    }

    // 変換中なら選択候補、そうでなければ読みのひらがなを確定形として返す
    private string SegmentAsFixed()
    {
        if (_romaji.Length == 0) return "";
        if (_selected >= 0 && _selected < _candidates.Count) return _candidates[_selected].Text;
        return CurrentReading();
    }
}
