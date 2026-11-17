namespace YoruKanaWin;

// ===== ユーザー辞書 編集フォーム =====
// トレイメニューから開く簡易編集画面。よみ＋単語の追加、一覧、削除を行う。
// 追加・削除は即座に userdict.json へ保存される (UserDictionaryFileStore)。
// 登録した単語は変換候補の最優先 (学習より前) に表示される。
public sealed class UserDictForm : Form
{
    private static readonly Color Bg = Color.FromArgb(13, 17, 35);
    private static readonly Color Panel = Color.FromArgb(26, 33, 64);
    private static readonly Color Fg = Color.FromArgb(232, 236, 248);
    private static readonly Color SubFg = Color.FromArgb(154, 165, 199);

    private readonly UserDictionaryFileStore _store;
    private readonly TextBox _readingBox = new();
    private readonly TextBox _wordBox = new();
    private readonly ListBox _listBox = new();
    private List<string[]> _entries = new();

    public UserDictForm(UserDictionaryFileStore store)
    {
        _store = store;

        Text = "よるかな - ユーザー辞書";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 400);
        BackColor = Bg;
        ForeColor = Fg;
        Font = new Font("Yu Gothic UI", 10f);

        var hint = new Label
        {
            Text = "登録した単語は変換候補のいちばん前に出ます",
            ForeColor = SubFg,
            AutoSize = true,
        };
        hint.SetBounds(12, 10, 396, 20);
        Controls.Add(hint);

        var readingLabel = new Label { Text = "よみ", ForeColor = Fg, AutoSize = true };
        readingLabel.SetBounds(12, 42, 40, 20);
        Controls.Add(readingLabel);
        _readingBox.SetBounds(56, 38, 150, 26);
        _readingBox.BackColor = Panel;
        _readingBox.ForeColor = Fg;
        Controls.Add(_readingBox);

        var wordLabel = new Label { Text = "単語", ForeColor = Fg, AutoSize = true };
        wordLabel.SetBounds(216, 42, 40, 20);
        Controls.Add(wordLabel);
        _wordBox.SetBounds(260, 38, 148, 26);
        _wordBox.BackColor = Panel;
        _wordBox.ForeColor = Fg;
        Controls.Add(_wordBox);

        var addButton = new Button { Text = "追加", ForeColor = Fg, BackColor = Panel, FlatStyle = FlatStyle.Flat };
        addButton.SetBounds(12, 74, 90, 30);
        addButton.Click += (_, _) => AddEntry();
        Controls.Add(addButton);

        var deleteButton = new Button { Text = "選択を削除", ForeColor = Fg, BackColor = Panel, FlatStyle = FlatStyle.Flat };
        deleteButton.SetBounds(112, 74, 110, 30);
        deleteButton.Click += (_, _) => DeleteSelected();
        Controls.Add(deleteButton);

        _listBox.SetBounds(12, 116, 396, 230);
        _listBox.BackColor = Panel;
        _listBox.ForeColor = Fg;
        _listBox.DoubleClick += (_, _) => DeleteSelected();
        Controls.Add(_listBox);

        var closeButton = new Button { Text = "閉じる", ForeColor = Fg, BackColor = Panel, FlatStyle = FlatStyle.Flat };
        closeButton.SetBounds(318, 356, 90, 30);
        closeButton.Click += (_, _) => Close();
        Controls.Add(closeButton);

        ReloadList();
    }

    private void ReloadList()
    {
        _entries = _store.Dictionary.Entries();
        _listBox.BeginUpdate();
        _listBox.Items.Clear();
        foreach (string[] e in _entries) _listBox.Items.Add($"{e[0]}　→　{e[1]}");
        _listBox.EndUpdate();
    }

    private void AddEntry()
    {
        string reading = _readingBox.Text.Trim();
        string word = _wordBox.Text.Trim();
        if (reading.Length == 0 || word.Length == 0) return;
        _store.Add(reading, word);
        _readingBox.Clear();
        _wordBox.Clear();
        _readingBox.Focus();
        ReloadList();
    }

    private void DeleteSelected()
    {
        int idx = _listBox.SelectedIndex;
        if (idx < 0 || idx >= _entries.Count) return;
        _store.Remove(_entries[idx][0], _entries[idx][1]);
        ReloadList();
    }
}
