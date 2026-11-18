using System.Text;
using System.Text.Json;

namespace YoruKanaWin;

// ===== ユーザー辞書 (純ロジック / GUI非依存) =====
// Android版 UserDictionary.kt の移植。よみ → 単語 の登録を保持する。
// 変換候補では内蔵辞書・学習より優先される (ConversionEngine 参照)。
// 登録順を保持する (Kotlin の LinkedHashMap 相当)。
public sealed class UserDictionary
{
    private readonly Dictionary<string, List<string>> _map = new();
    private readonly List<string> _keyOrder = new();

    // 単語を追加する。空文字・重複は無視する。よみ/単語内のタブと改行は空白に置換する。
    public void Add(string? reading, string? word)
    {
        reading = Sanitize(reading);
        word = Sanitize(word);
        if (reading.Length == 0 || word.Length == 0) return;
        if (!_map.TryGetValue(reading, out List<string>? words))
        {
            words = new List<string>();
            _map[reading] = words;
            _keyOrder.Add(reading);
        }
        if (!words.Contains(word)) words.Add(word);
    }

    // 単語を削除する。削除できたら true。よみが空になったら読み自体も消す。
    public bool Remove(string? reading, string? word)
    {
        if (reading == null || word == null) return false;
        if (!_map.TryGetValue(reading, out List<string>? words)) return false;
        bool removed = words.Remove(word);
        if (words.Count == 0)
        {
            _map.Remove(reading);
            _keyOrder.Remove(reading);
        }
        return removed;
    }

    // よみに対する単語一覧 (登録順)。無ければ空リスト。
    public List<string> Lookup(string reading)
    {
        return _map.TryGetValue(reading, out List<string>? words)
            ? new List<string>(words)
            : new List<string>();
    }

    public bool Has(string reading) => _map.ContainsKey(reading);

    // (よみ, 単語) ペアの一覧を登録順に返す (編集画面の一覧表示用)。
    public List<string[]> Entries()
    {
        var outList = new List<string[]>();
        foreach (string reading in _keyOrder)
        {
            foreach (string w in _map[reading]) outList.Add(new[] { reading, w });
        }
        return outList;
    }

    // 登録単語の総数
    public int Size
    {
        get
        {
            int n = 0;
            foreach (var v in _map.Values) n += v.Count;
            return n;
        }
    }

    public void Clear()
    {
        _map.Clear();
        _keyOrder.Clear();
    }

    private static string Sanitize(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim();
    }
}

// ===== ユーザー辞書 (JSONファイル永続化) =====
// 保存先: %AppData%\YoruKana\userdict.json
// 形式: { "よみ": ["単語", ...], ... } (System.Text.Json、学習と同じ流儀)
// 書き込みは一時ファイル経由のアトミック置換。読み込み時は不正な内容を除去する。
public sealed class UserDictionaryFileStore
{
    private readonly string _filePath;

    // 変換エンジンと共有する実体。編集はこのインスタンス経由で行い、都度保存する。
    public UserDictionary Dictionary { get; } = new();

    public UserDictionaryFileStore() : this(DefaultPath()) { }

    public UserDictionaryFileStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public static string DefaultPath()
        => Path.Combine(LearningFileStore.AppDataDir(), "userdict.json");

    // 追加して保存する (空・重複は UserDictionary 側で無視される)
    public void Add(string? reading, string? word)
    {
        int before = Dictionary.Size;
        Dictionary.Add(reading, word);
        if (Dictionary.Size != before) Save();
    }

    // 削除して保存する。削除できたら true。
    public bool Remove(string? reading, string? word)
    {
        bool removed = Dictionary.Remove(reading, word);
        if (removed) Save();
        return removed;
    }

    public void Clear()
    {
        Dictionary.Clear();
        Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            string json = File.ReadAllText(_filePath, Encoding.UTF8);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            if (parsed == null) return;
            foreach (var kv in parsed)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;
                foreach (string? word in kv.Value)
                {
                    if (!string.IsNullOrEmpty(word)) Dictionary.Add(kv.Key, word);
                }
            }
        }
        catch (Exception)
        {
            // 壊れた辞書ファイルは空として扱い、起動を妨げない
            Dictionary.Clear();
        }
    }

    private void Save()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var data = new Dictionary<string, List<string>>();
            foreach (string[] e in Dictionary.Entries())
            {
                if (!data.TryGetValue(e[0], out List<string>? list))
                {
                    list = new List<string>();
                    data[e[0]] = list;
                }
                list.Add(e[1]);
            }
            string tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(data), Encoding.UTF8);
            File.Move(tmp, _filePath, overwrite: true);
        }
        catch (Exception)
        {
            // 保存に失敗しても入力自体は継続できるようにする
        }
    }
}
