namespace YoruKanaWin;

// ===== よるかな 変換エンジン (GUI非依存 / Android版 Kotlin からの忠実移植) =====
// 移植元: yorukana-ime の RomajiConverter.kt / KanaUtils.kt / Dictionary.kt /
// ConversionEngine.kt / LearningStore.kt / MemoryLearningStore.kt。
// 仕様・候補順・活用生成は Kotlin 版と同一。クラス名のみ C# の慣例に合わせた
// (Dictionary -> JapaneseDictionary、LearningStore -> ILearningStore)。

// ----- ローマ字→ひらがな変換 -----
// composing 入力途中の未確定ローマ字も扱う: 変換済み部分 (Kana) と
// まだ確定できない末尾ローマ字 (Pending) を分けて返す。
public static class RomajiConverter
{
    public sealed class Result
    {
        public string Kana { get; }    // 変換済みのひらがな
        public string Pending { get; } // 未確定の末尾ローマ字 (例: "k", "ky", "n", "sh")
        internal Result(string kana, string pending) { Kana = kana; Pending = pending; }
    }

    private static readonly Dictionary<string, string> Table = new();

    private static void Put(string roma, string kana) => Table[roma] = kana;

    static RomajiConverter()
    {
        // 基本 (五段)
        Put("a", "あ"); Put("i", "い"); Put("u", "う"); Put("e", "え"); Put("o", "お");
        Put("ka", "か"); Put("ki", "き"); Put("ku", "く"); Put("ke", "け"); Put("ko", "こ");
        Put("ga", "が"); Put("gi", "ぎ"); Put("gu", "ぐ"); Put("ge", "げ"); Put("go", "ご");
        Put("sa", "さ"); Put("shi", "し"); Put("si", "し"); Put("su", "す"); Put("se", "せ"); Put("so", "そ");
        Put("za", "ざ"); Put("ji", "じ"); Put("zi", "じ"); Put("zu", "ず"); Put("ze", "ぜ"); Put("zo", "ぞ");
        Put("ta", "た"); Put("chi", "ち"); Put("tsu", "つ"); Put("tu", "つ"); Put("te", "て"); Put("to", "と");
        Put("da", "だ"); Put("du", "づ"); Put("de", "で"); Put("do", "ど");
        Put("na", "な"); Put("ni", "に"); Put("nu", "ぬ"); Put("ne", "ね"); Put("no", "の");
        Put("ha", "は"); Put("hi", "ひ"); Put("fu", "ふ"); Put("hu", "ふ"); Put("he", "へ"); Put("ho", "ほ");
        Put("ba", "ば"); Put("bi", "び"); Put("bu", "ぶ"); Put("be", "べ"); Put("bo", "ぼ");
        Put("pa", "ぱ"); Put("pi", "ぴ"); Put("pu", "ぷ"); Put("pe", "ぺ"); Put("po", "ぽ");
        Put("ma", "ま"); Put("mi", "み"); Put("mu", "む"); Put("me", "め"); Put("mo", "も");
        Put("ya", "や"); Put("yu", "ゆ"); Put("yo", "よ");
        Put("ra", "ら"); Put("ri", "り"); Put("ru", "る"); Put("re", "れ"); Put("ro", "ろ");
        Put("wa", "わ"); Put("wo", "を");
        // 拗音
        Put("kya", "きゃ"); Put("kyu", "きゅ"); Put("kyo", "きょ");
        Put("gya", "ぎゃ"); Put("gyu", "ぎゅ"); Put("gyo", "ぎょ");
        Put("sha", "しゃ"); Put("shu", "しゅ"); Put("sho", "しょ");
        Put("sya", "しゃ"); Put("syu", "しゅ"); Put("syo", "しょ");
        Put("ja", "じゃ"); Put("ju", "じゅ"); Put("jo", "じょ");
        Put("jya", "じゃ"); Put("jyu", "じゅ"); Put("jyo", "じょ");
        Put("zya", "じゃ"); Put("zyu", "じゅ"); Put("zyo", "じょ");
        Put("cha", "ちゃ"); Put("chu", "ちゅ"); Put("cho", "ちょ");
        Put("tya", "ちゃ"); Put("tyu", "ちゅ"); Put("tyo", "ちょ");
        Put("cya", "ちゃ"); Put("cyu", "ちゅ"); Put("cyo", "ちょ");
        Put("nya", "にゃ"); Put("nyu", "にゅ"); Put("nyo", "にょ");
        Put("hya", "ひゃ"); Put("hyu", "ひゅ"); Put("hyo", "ひょ");
        Put("bya", "びゃ"); Put("byu", "びゅ"); Put("byo", "びょ");
        Put("pya", "ぴゃ"); Put("pyu", "ぴゅ"); Put("pyo", "ぴょ");
        Put("mya", "みゃ"); Put("myu", "みゅ"); Put("myo", "みょ");
        Put("rya", "りゃ"); Put("ryu", "りゅ"); Put("ryo", "りょ");
        Put("dya", "ぢゃ"); Put("dyu", "ぢゅ"); Put("dyo", "ぢょ");
        // 外来音 (Java版仕様準拠: ti/di は てぃ/でぃ)
        Put("fa", "ふぁ"); Put("fi", "ふぃ"); Put("fe", "ふぇ"); Put("fo", "ふぉ"); Put("fyu", "ふゅ");
        Put("ti", "てぃ"); Put("di", "でぃ");
        Put("wi", "うぃ"); Put("we", "うぇ");
        Put("va", "ゔぁ"); Put("vi", "ゔぃ"); Put("vu", "ゔ"); Put("ve", "ゔぇ"); Put("vo", "ゔぉ");
        Put("she", "しぇ"); Put("je", "じぇ"); Put("che", "ちぇ");
        Put("tsa", "つぁ"); Put("tsi", "つぃ"); Put("tse", "つぇ"); Put("tso", "つぉ");
        Put("kwa", "くぁ"); Put("gwa", "ぐぁ");
        // 小書き (x / l 系)
        Put("xa", "ぁ"); Put("xi", "ぃ"); Put("xu", "ぅ"); Put("xe", "ぇ"); Put("xo", "ぉ");
        Put("la", "ぁ"); Put("li", "ぃ"); Put("lu", "ぅ"); Put("le", "ぇ"); Put("lo", "ぉ");
        Put("xya", "ゃ"); Put("xyu", "ゅ"); Put("xyo", "ょ");
        Put("lya", "ゃ"); Put("lyu", "ゅ"); Put("lyo", "ょ");
        Put("xtu", "っ"); Put("ltu", "っ"); Put("xtsu", "っ"); Put("ltsu", "っ");
        Put("xwa", "ゎ"); Put("lwa", "ゎ");
        Put("xka", "ヵ"); Put("xke", "ヶ");
    }

    private static bool IsVowel(char c) => c is 'a' or 'i' or 'u' or 'e' or 'o';

    private static bool IsConsonant(char c) => c is >= 'a' and <= 'z' && !IsVowel(c);

    // rest が何らかの変換キーの接頭辞なら「まだ続きがあり得る」= pending
    private static bool IsPossiblePrefix(string rest)
    {
        if (rest.Length == 0) return false;
        foreach (string key in Table.Keys)
        {
            if (key.StartsWith(rest, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    // ローマ字文字列を左から変換し、確定できない末尾を Pending として返す。
    public static Result Convert(string input)
    {
        var sb = new System.Text.StringBuilder();
        string pending = "";
        int n = input.Length;
        int i = 0;
        while (i < n)
        {
            char c = input[i];
            if (c == '-') { sb.Append('ー'); i++; continue; }
            if (c < 'a' || c > 'z')
            {
                if (c == '\'') { i++; continue; } // 単独の ' は読み飛ばす (n' は n 側で処理)
                sb.Append(c); i++; continue;
            }
            // 促音: "tch" (matcha 等)。Java版 startsWith("tch", i) と等価
            if (c == 't' && i + 2 < n && input[i + 1] == 'c' && input[i + 2] == 'h')
            {
                sb.Append('っ'); i++; continue;
            }
            // 促音: 子音の重ね (n を除く)
            if (i + 1 < n && input[i + 1] == c && IsConsonant(c) && c != 'n')
            {
                sb.Append('っ'); i++; continue;
            }
            // 撥音 n の処理
            if (c == 'n')
            {
                if (i + 1 >= n) { pending = "n"; i = n; break; }
                char nx = input[i + 1];
                if (nx == '\'') { sb.Append('ん'); i += 2; continue; }
                if (nx == 'n')
                {
                    if (i + 2 < n && (IsVowel(input[i + 2]) || input[i + 2] == 'y'))
                    {
                        sb.Append('ん'); i += 1; continue; // ん + な行 (nna → んな)
                    }
                    sb.Append('ん'); i += 2; continue;
                }
                if (!IsVowel(nx) && nx != 'y') { sb.Append('ん'); i += 1; continue; }
                // 母音 / y が続く場合はテーブル (na / nya 等) に委ねる
            }
            // テーブル最長一致 (最長キーは "xtsu"/"ltsu" の4文字)
            string? matched = null;
            int mlen = 0;
            for (int len = Math.Min(4, n - i); len >= 1; len--)
            {
                if (Table.TryGetValue(input.Substring(i, len), out string? k)) { matched = k; mlen = len; break; }
            }
            if (matched != null) { sb.Append(matched); i += mlen; continue; }
            // 変換できない位置: 残りがキーの接頭辞なら pending、それ以外は文字を素通し
            string rest = input.Substring(i);
            if (IsPossiblePrefix(rest)) { pending = rest; break; }
            sb.Append(c); i++;
        }
        return new Result(sb.ToString(), pending);
    }

    // 完全変換 (確定時): pending の末尾 n は ん に、それ以外はローマ字のまま残す。
    public static string ConvertFully(string input)
    {
        Result r = Convert(input);
        if (r.Pending.Length == 0) return r.Kana;
        if (r.Pending == "n") return r.Kana + "ん";
        return r.Kana + r.Pending;
    }
}

// ----- かなユーティリティ -----
// ひらがな⇔カタカナ変換、濁点/半濁点/小書きトグル、12キー段循環表。
public static class KanaUtils
{
    // ぁ(0x3041)〜ゖ(0x3096) と ァ(0x30A1)〜ヶ(0x30F6) は 0x60 差で対応する。
    public static string ToKatakana(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char ch in s)
        {
            char c = ch;
            if (c >= 'ぁ' && c <= 'ゖ') c = (char)(c + 0x60);
            sb.Append(c);
        }
        return sb.ToString();
    }

    public static string ToHiragana(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char ch in s)
        {
            char c = ch;
            if (c >= 'ァ' && c <= 'ヶ') c = (char)(c - 0x60);
            sb.Append(c);
        }
        return sb.ToString();
    }

    // 濁点・半濁点・小書きトグル: 1文字を次の形へ循環させる。対象外はそのまま。
    // 例: か→が→か / は→ば→ぱ→は / つ→づ→っ→つ / あ→ぁ→あ / う→ゔ→ぅ→う
    private static readonly Dictionary<char, char> Toggle = new();

    private static void Cycle(params char[] chars)
    {
        for (int i = 0; i < chars.Length; i++)
        {
            Toggle[chars[i]] = chars[(i + 1) % chars.Length];
        }
    }

    static KanaUtils()
    {
        Cycle('か', 'が'); Cycle('き', 'ぎ'); Cycle('く', 'ぐ'); Cycle('け', 'げ'); Cycle('こ', 'ご');
        Cycle('さ', 'ざ'); Cycle('し', 'じ'); Cycle('す', 'ず'); Cycle('せ', 'ぜ'); Cycle('そ', 'ぞ');
        Cycle('た', 'だ'); Cycle('ち', 'ぢ'); Cycle('て', 'で'); Cycle('と', 'ど');
        Cycle('は', 'ば', 'ぱ'); Cycle('ひ', 'び', 'ぴ'); Cycle('ふ', 'ぶ', 'ぷ');
        Cycle('へ', 'べ', 'ぺ'); Cycle('ほ', 'ぼ', 'ぽ');
        Cycle('つ', 'づ', 'っ');
        Cycle('う', 'ゔ', 'ぅ'); // う→ゔ→ぅ→う
        Cycle('あ', 'ぁ'); Cycle('い', 'ぃ');
        Cycle('え', 'ぇ'); Cycle('お', 'ぉ');
        Cycle('や', 'ゃ'); Cycle('ゆ', 'ゅ'); Cycle('よ', 'ょ');
        Cycle('わ', 'ゎ');
    }

    public static char ToggleDakuten(char c) => Toggle.TryGetValue(c, out char next) ? next : c;

    public static bool CanToggle(char c) => Toggle.ContainsKey(c);

    // 12キー (トグル打ち) の段循環表
    public static readonly string[] ToggleRows =
    {
        "あいうえお", // あ行
        "かきくけこ", // か行
        "さしすせそ", // さ行
        "たちつてと", // た行
        "なにぬねの", // な行
        "はひふへほ", // は行
        "まみむめも", // ま行
        "やゆよ",     // や行
        "らりるれろ", // ら行
        "わをんー〜", // わ行
    };

    // 文字が属する行のインデックス (属さなければ -1)
    public static int RowIndexOf(char c)
    {
        for (int r = 0; r < ToggleRows.Length; r++)
        {
            if (ToggleRows[r].IndexOf(c) >= 0) return r;
        }
        return -1;
    }

    // 行内での次の文字 (末尾なら先頭へ)
    public static char NextInRow(int row, char current)
    {
        string r = ToggleRows[row];
        int idx = r.IndexOf(current);
        if (idx < 0) return r[0];
        return r[(idx + 1) % r.Length];
    }
}

// ----- 学習ストア (インターフェース) -----
// 確定した候補の頻度を保持し、候補ソートに反映するための抽象。
public interface ILearningStore
{
    int GetFrequency(string reading, string candidate);
    void RecordSelection(string reading, string candidate);
    void Clear();
}

// ----- 学習ストア (メモリ実装) -----
// キーは 読み + 区切り + 候補。
public class MemoryLearningStore : ILearningStore
{
    internal const string Sep = "\u0001";

    protected readonly Dictionary<string, int> Freq = new();

    internal static string Key(string reading, string candidate) => reading + Sep + candidate;

    public virtual int GetFrequency(string reading, string candidate)
        => Freq.TryGetValue(Key(reading, candidate), out int v) ? v : 0;

    public virtual void RecordSelection(string reading, string candidate)
    {
        string k = Key(reading, candidate);
        Freq[k] = GetFrequency(reading, candidate) + 1;
    }

    public virtual void Clear() => Freq.Clear();

    // 永続化レイヤが復元/保存に使う生マップ
    public Dictionary<string, int> RawMap() => Freq;
}

// ----- 内蔵辞書 -----
// DictionaryData の明示エントリに加え、動詞・形容詞の活用形を生成して登録する。
public sealed class JapaneseDictionary
{
    private readonly Dictionary<string, List<string>> _map = new();
    private readonly List<string> _keyOrder = new(); // Java LinkedHashMap の挿入順を保持
    private readonly Dictionary<string, List<string>> _singleKanji = new();

    public JapaneseDictionary()
    {
        Build();
    }

    private void Add(string reading, string candidate)
    {
        if (string.IsNullOrEmpty(reading) || string.IsNullOrEmpty(candidate)) return;
        if (!_map.TryGetValue(reading, out List<string>? list))
        {
            list = new List<string>();
            _map[reading] = list;
            _keyOrder.Add(reading);
        }
        if (!list.Contains(candidate)) list.Add(candidate);
    }

    private void Build()
    {
        foreach (string e in DictionaryData.ENTRIES)
        {
            int eq = e.IndexOf('=');
            if (eq < 0) continue;
            string reading = e.Substring(0, eq);
            foreach (string cand in e.Substring(eq + 1).Split(',')) Add(reading, cand);
        }
        foreach (string v in DictionaryData.GODAN)
        {
            int bar = v.IndexOf('|');
            GenGodan(v.Substring(0, bar), v.Substring(bar + 1));
        }
        foreach (string v in DictionaryData.ICHIDAN)
        {
            int bar = v.IndexOf('|');
            GenIchidan(v.Substring(0, bar), v.Substring(bar + 1));
        }
        foreach (string a in DictionaryData.I_ADJ)
        {
            int bar = a.IndexOf('|');
            GenIAdj(a.Substring(0, bar), a.Substring(bar + 1));
        }
        foreach (string a in DictionaryData.NA_ADJ)
        {
            int bar = a.IndexOf('|');
            GenNaAdj(a.Substring(0, bar), a.Substring(bar + 1));
        }
        foreach (string e in DictionaryData.SINGLE_KANJI)
        {
            int eq = e.IndexOf('=');
            if (eq < 0) continue;
            var list = new List<string>(e.Substring(eq + 1).Split(','));
            _singleKanji[e.Substring(0, eq)] = list;
        }
    }

    // 五段動詞の活用行: 語尾 → {い段, あ段, え段, お段, た形語尾, て形語尾}
    private static readonly Dictionary<char, string[]> GodanRows = new()
    {
        ['く'] = new[] { "き", "か", "け", "こ", "いた", "いて" },
        ['ぐ'] = new[] { "ぎ", "が", "げ", "ご", "いだ", "いで" },
        ['す'] = new[] { "し", "さ", "せ", "そ", "した", "して" },
        ['つ'] = new[] { "ち", "た", "て", "と", "った", "って" },
        ['る'] = new[] { "り", "ら", "れ", "ろ", "った", "って" },
        ['う'] = new[] { "い", "わ", "え", "お", "った", "って" },
        ['ぬ'] = new[] { "に", "な", "ね", "の", "んだ", "んで" },
        ['ぶ'] = new[] { "び", "ば", "べ", "ぼ", "んだ", "んで" },
        ['む'] = new[] { "み", "ま", "め", "も", "んだ", "んで" },
    };

    private void GenGodan(string reading, string kanji)
    {
        if (reading.Length < 2 || kanji.Length < 2) return;
        char last = reading[reading.Length - 1];
        if (!GodanRows.TryGetValue(last, out string[]? row)) return;
        string rs = reading.Substring(0, reading.Length - 1); // 読みの語幹
        string ks = kanji.Substring(0, kanji.Length - 1);     // 漢字形の語幹
        Add(reading, kanji);                                    // 辞書形
        Add(rs + row[0] + "ます", ks + row[0] + "ます");        // ます形
        Add(rs + row[0] + "ました", ks + row[0] + "ました");    // ました
        Add(rs + row[0] + "たい", ks + row[0] + "たい");        // たい
        string taR, teR, taK, teK;
        if (reading == "いく") { // 行く は例外 (いった/いって)
            taR = "いった"; teR = "いって"; taK = "行った"; teK = "行って";
        } else {
            taR = rs + row[4]; teR = rs + row[5]; taK = ks + row[4]; teK = ks + row[5];
        }
        Add(taR, taK);                                          // た形
        Add(teR, teK);                                          // て形
        Add(rs + row[1] + "ない", ks + row[1] + "ない");        // ない形
        Add(rs + row[2] + "る", ks + row[2] + "る");            // 可能形
        Add(rs + row[3] + "う", ks + row[3] + "う");            // 意向形
    }

    private void GenIchidan(string reading, string kanji)
    {
        if (reading.Length < 2 || !reading.EndsWith("る", StringComparison.Ordinal)) return;
        string rs = reading.Substring(0, reading.Length - 1);
        string ks = kanji.Substring(0, kanji.Length - 1);
        Add(reading, kanji);                                    // 辞書形
        Add(rs + "ます", ks + "ます");
        Add(rs + "ました", ks + "ました");
        Add(rs + "た", ks + "た");                              // た形
        Add(rs + "て", ks + "て");                              // て形
        Add(rs + "ない", ks + "ない");                          // ない形
        Add(rs + "たい", ks + "たい");                          // たい
        Add(rs + "られる", ks + "られる");                      // 可能/受身
        Add(rs + "よう", ks + "よう");                          // 意向形
    }

    private void GenIAdj(string reading, string kanji)
    {
        if (reading.Length < 2 || !reading.EndsWith("い", StringComparison.Ordinal)) return;
        string rs = reading.Substring(0, reading.Length - 1);
        string ks = kanji.Substring(0, kanji.Length - 1);
        Add(reading, kanji);                                    // 終止形
        Add(rs + "くない", ks + "くない");                      // 否定
        Add(rs + "かった", ks + "かった");                      // 過去
        Add(rs + "くて", ks + "くて");                          // て形
        Add(rs + "く", ks + "く");                              // 連用
        Add(rs + "さ", ks + "さ");                              // 名詞化 (高さ等)
    }

    private void GenNaAdj(string reading, string kanji)
    {
        Add(reading, kanji);                                    // 語幹
        Add(reading + "な", kanji + "な");                      // 連体
        Add(reading + "に", kanji + "に");                      // 連用
        Add(reading + "だ", kanji + "だ");                      // 終止
        Add(reading + "です", kanji + "です");                  // 丁寧
        Add(reading + "だった", kanji + "だった");              // 過去
    }

    // ----- 検索 -----
    public bool Has(string reading) => _map.ContainsKey(reading);

    // 読みに対する候補 (無ければ空リスト)。単漢字フォールバックも含む。
    public List<string> Lookup(string reading)
    {
        var result = new List<string>();
        if (_map.TryGetValue(reading, out List<string>? hit)) result.AddRange(hit);
        if (result.Count == 0 && reading.Length == 1)
        {
            if (_singleKanji.TryGetValue(reading, out List<string>? sk)) result.AddRange(sk);
        }
        return result;
    }

    // 前方一致する読みの一覧 (予測変換用)。挿入順で返す。
    public List<string> ReadingsWithPrefix(string prefix)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(prefix)) return result;
        foreach (string key in _keyOrder)
        {
            if (key.Length > prefix.Length && key.StartsWith(prefix, StringComparison.Ordinal)) result.Add(key);
        }
        return result;
    }

    // 登録されている読みの総数 (辞書語数)
    public int Size() => _map.Count;
}

// ----- 変換エンジン -----
// 候補生成順: ⓪ユーザー辞書 (最優先) ①辞書候補 (学習頻度でソート)
// ②カタカナ ③ひらがな原文 ④ローマ字原文。
// 末尾助詞の簡易分離変換と、前方一致の予測候補も提供する。
public sealed class ConversionEngine
{
    // 末尾助詞として分離を試みる文字
    private const string Particles = "はがをにへとのかもねよ";

    private readonly JapaneseDictionary _dictionary;
    private readonly ILearningStore? _learning;
    private UserDictionary? _userDictionary;

    public ConversionEngine(JapaneseDictionary dictionary, ILearningStore? learning)
        : this(dictionary, learning, null)
    {
    }

    public ConversionEngine(JapaneseDictionary dictionary, ILearningStore? learning,
        UserDictionary? userDictionary)
    {
        _dictionary = dictionary;
        _learning = learning;
        _userDictionary = userDictionary;
    }

    public JapaneseDictionary Dictionary => _dictionary;

    // ユーザー辞書を差し替える (アプリ側が読み込み後に差し替える場合)
    public void SetUserDictionary(UserDictionary? userDictionary) => _userDictionary = userDictionary;

    public UserDictionary? UserDict => _userDictionary;

    // 学習頻度が高い順に安定ソート
    private List<string> SortByFreq(string reading, List<string> cands)
    {
        // LINQ の OrderByDescending は安定ソート (Java の List.sort と同じ)
        return cands.OrderByDescending(c => _learning?.GetFrequency(reading, c) ?? 0).ToList();
    }

    // ----- 候補生成 -----
    // reading: ひらがな読み / romajiRaw: ローマ字原文 (かな入力時は null でよい)
    public List<string> Candidates(string? reading, string? romajiRaw)
    {
        var set = new LinkedHashSet<string>();
        if (string.IsNullOrEmpty(reading)) return new List<string>();

        // ⓪ユーザー辞書が最優先 (学習より前、登録順のまま)
        if (_userDictionary != null)
        {
            foreach (string c in _userDictionary.Lookup(reading)) set.Add(c);
        }

        List<string> dict = _dictionary.Lookup(reading);
        if (dict.Count > 0)
        {
            foreach (string c in SortByFreq(reading, dict)) set.Add(c);
        }
        else
        {
            // 末尾助詞の簡易分離: 語幹が辞書にあれば 語幹候補+助詞 を提示
            string? stem = SplitParticleCandidate(reading);
            if (stem != null)
            {
                string particle = reading.Substring(reading.Length - 1);
                List<string> stemCands = SortByFreq(reading,
                    WithSuffix(_dictionary.Lookup(stem), particle));
                foreach (string c in stemCands) set.Add(c);
            }
        }
        set.Add(KanaUtils.ToKatakana(reading)); // ②カタカナ
        set.Add(reading);                        // ③ひらがな原文
        if (!string.IsNullOrEmpty(romajiRaw))
        {
            set.Add(romajiRaw);                  // ④ローマ字原文
        }
        return new List<string>(set);
    }

    private static List<string> WithSuffix(List<string> baseList, string suffix)
    {
        var result = new List<string>(baseList.Count);
        foreach (string b in baseList) result.Add(b + suffix);
        return result;
    }

    // 分離変換が可能なら語幹を返す (不可なら null)
    private string? SplitParticleCandidate(string reading)
    {
        if (reading.Length < 3) return null; // 語幹2文字以上を要求
        char last = reading[reading.Length - 1];
        if (Particles.IndexOf(last) < 0) return null;
        string stem = reading.Substring(0, reading.Length - 1);
        return _dictionary.Has(stem) ? stem : null;
    }

    // ----- 予測候補 (前方一致) -----
    // prefix で始まる読みの代表候補を返す。頻度→読みの短さ の順。
    public List<string> Predict(string? prefix, int limit)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(prefix) || limit <= 0) return result;
        List<string> readings = _dictionary.ReadingsWithPrefix(prefix);
        var sorted = readings
            .OrderByDescending(r => BestFreq(r))
            .ThenBy(r => r.Length)
            .ToList();
        var seen = new LinkedHashSet<string>();
        foreach (string r in sorted)
        {
            List<string> cands = _dictionary.Lookup(r);
            if (cands.Count == 0) continue;
            string best = SortByFreq(r, cands)[0];
            if (seen.Add(best)) result.Add(best);
            if (result.Count >= limit) break;
        }
        return result;
    }

    // 予測候補を (読み, 候補) ペアで返す (学習記録に正しい読みを使うため)
    public List<string[]> PredictEntries(string? prefix, int limit)
    {
        var result = new List<string[]>();
        if (string.IsNullOrEmpty(prefix) || limit <= 0) return result;
        List<string> readings = _dictionary.ReadingsWithPrefix(prefix);
        var sorted = readings
            .OrderByDescending(r => BestFreq(r))
            .ThenBy(r => r.Length)
            .ToList();
        var seen = new LinkedHashSet<string>();
        foreach (string r in sorted)
        {
            List<string> cands = _dictionary.Lookup(r);
            if (cands.Count == 0) continue;
            string best = SortByFreq(r, cands)[0];
            if (seen.Add(best)) result.Add(new[] { r, best });
            if (result.Count >= limit) break;
        }
        return result;
    }

    private int BestFreq(string reading)
    {
        if (_learning == null) return 0;
        int best = 0;
        foreach (string c in _dictionary.Lookup(reading))
        {
            best = Math.Max(best, _learning.GetFrequency(reading, c));
        }
        return best;
    }
}

// Java の LinkedHashSet 相当 (挿入順を保つ集合)。候補の重複排除に使う。
internal sealed class LinkedHashSet<T> : IEnumerable<T> where T : notnull
{
    private readonly Dictionary<T, byte> _map = new();
    private readonly List<T> _order = new();

    public bool Add(T item)
    {
        if (_map.ContainsKey(item)) return false;
        _map[item] = 0;
        _order.Add(item);
        return true;
    }

    public IEnumerator<T> GetEnumerator() => _order.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
