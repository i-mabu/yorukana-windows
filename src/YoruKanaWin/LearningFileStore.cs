using System.Text;
using System.Text.Json;

namespace YoruKanaWin;

// ===== 学習ストア (JSONファイル永続化実装) =====
// 保存先: %AppData%\YoruKana\learning.json
// 形式: { "読み": { "候補": 回数, ... }, ... } (System.Text.Json)
// 書き込みは一時ファイル経由のアトミック置換 (書き込み途中で終了しても壊れない)。
public sealed class LearningFileStore : ILearningStore
{
    private readonly Dictionary<string, Dictionary<string, int>> _data = new();
    private readonly string _filePath;

    public LearningFileStore() : this(DefaultPath()) { }

    public LearningFileStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public static string AppDataDir()
    {
        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(baseDir, "YoruKana");
    }

    public static string DefaultPath() => Path.Combine(AppDataDir(), "learning.json");

    public int GetFrequency(string reading, string candidate)
    {
        if (_data.TryGetValue(reading, out Dictionary<string, int>? inner)
            && inner.TryGetValue(candidate, out int v))
        {
            return v;
        }
        return 0;
    }

    public void RecordSelection(string reading, string candidate)
    {
        if (!_data.TryGetValue(reading, out Dictionary<string, int>? inner))
        {
            inner = new Dictionary<string, int>();
            _data[reading] = inner;
        }
        inner[candidate] = GetFrequency(reading, candidate) + 1;
        Save();
    }

    public void Clear()
    {
        _data.Clear();
        Save();
    }

    // テスト・診断用: 登録されている読みの数
    public int ReadingCount => _data.Count;

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            string json = File.ReadAllText(_filePath, Encoding.UTF8);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);
            if (parsed == null) return;
            // 正規化: 手編集や旧バージョン由来の不正な内容を読み込み時に除去する
            // (null の内側辞書、空/null の読み・候補、負の回数、空になった読み)
            foreach (var kv in parsed)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                if (kv.Value == null) continue;
                var inner = new Dictionary<string, int>();
                foreach (var cand in kv.Value)
                {
                    if (string.IsNullOrEmpty(cand.Key)) continue;
                    if (cand.Value < 0) continue;
                    inner[cand.Key] = cand.Value;
                }
                if (inner.Count > 0) _data[kv.Key] = inner;
            }
        }
        catch (Exception)
        {
            // 壊れた学習ファイルは空として扱い、アプリの起動を妨げない
            _data.Clear();
        }
    }

    private void Save()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmp = _filePath + ".tmp";
            string json = JsonSerializer.Serialize(_data);
            File.WriteAllText(tmp, json, Encoding.UTF8);
            File.Move(tmp, _filePath, overwrite: true);
        }
        catch (Exception)
        {
            // 保存に失敗しても入力自体は継続できるようにする
        }
    }
}
