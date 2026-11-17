using System.Runtime.InteropServices;
using YoruKanaWin;

// ===== YoruKanaTest (コンソール方式の単体テスト・外部依存なし) =====
// Android版 tests/TestMain.java のチェック項目を C# に移植したもの。
// 追加で「辞書読み数が Android版と一致して 3,502 であること」と
// 「学習の JSON 永続化 (LearningFileStore) の往復」を検証する。

int passed = 0;

void Check(bool cond, string name)
{
    if (!cond) throw new Exception("FAILED: " + name);
    passed++;
    Console.WriteLine("ok: " + name);
}

void Eq(string actual, string expected, string name)
    => Check(expected == actual, $"{name} (expected={expected}, actual={actual})");

try
{
    // ---------- RomajiConverter ----------
    RomajiConverter.Result r;

    r = RomajiConverter.Convert("ohayou");
    Eq(r.Kana, "おはよう", "romaji ohayou");
    Eq(r.Pending, "", "romaji ohayou pending");

    r = RomajiConverter.Convert("gakkou");
    Eq(r.Kana, "がっこう", "romaji sokuon gakkou");

    r = RomajiConverter.Convert("kani");
    Eq(r.Kana, "かに", "romaji kani (n before vowel)");
    Eq(r.Pending, "", "romaji kani pending");

    r = RomajiConverter.Convert("kan");
    Eq(r.Kana, "か", "romaji kan kana");
    Eq(r.Pending, "n", "romaji kan pending n");

    r = RomajiConverter.Convert("kann");
    Eq(r.Kana, "かん", "romaji kann -> kan");
    Eq(r.Pending, "", "romaji kann pending");

    r = RomajiConverter.Convert("kan'i");
    Eq(r.Kana, "かんい", "romaji kan'i");

    r = RomajiConverter.Convert("nna");
    Eq(r.Kana, "んな", "romaji nna");

    r = RomajiConverter.Convert("kyou");
    Eq(r.Kana, "きょう", "romaji youon kyou");

    r = RomajiConverter.Convert("shashin");
    Eq(r.Kana, "しゃし", "romaji shashin kana (final n pending)");
    Eq(r.Pending, "n", "romaji shashin pending n");
    Eq(RomajiConverter.ConvertFully("shashin"), "しゃしん", "romaji shashin fully");

    r = RomajiConverter.Convert("matcha");
    Eq(r.Kana, "まっちゃ", "romaji matcha (tch sokuon)");

    r = RomajiConverter.Convert("fa");
    Eq(r.Kana, "ふぁ", "romaji foreign fa");
    r = RomajiConverter.Convert("ti");
    Eq(r.Kana, "てぃ", "romaji foreign ti (SPEC)");
    r = RomajiConverter.Convert("di");
    Eq(r.Kana, "でぃ", "romaji foreign di (SPEC)");
    r = RomajiConverter.Convert("wi");
    Eq(r.Kana, "うぃ", "romaji foreign wi");

    r = RomajiConverter.Convert("xtu");
    Eq(r.Kana, "っ", "romaji xtu");
    r = RomajiConverter.Convert("ltu");
    Eq(r.Kana, "っ", "romaji ltu");
    r = RomajiConverter.Convert("axtu");
    Eq(r.Kana, "あっ", "romaji axtu");

    r = RomajiConverter.Convert("k");
    Eq(r.Kana, "", "romaji k kana empty");
    Eq(r.Pending, "k", "romaji k pending");
    r = RomajiConverter.Convert("ky");
    Eq(r.Pending, "ky", "romaji ky pending");
    r = RomajiConverter.Convert("kak");
    Eq(r.Kana, "か", "romaji kak kana");
    Eq(r.Pending, "k", "romaji kak pending");
    r = RomajiConverter.Convert("sh");
    Eq(r.Pending, "sh", "romaji sh pending");

    Eq(RomajiConverter.ConvertFully("kan"), "かん", "convertFully kan flushes n");
    Eq(RomajiConverter.ConvertFully("sensei"), "せんせい", "convertFully sensei");
    Eq(RomajiConverter.ConvertFully("shinbun"), "しんぶん", "convertFully shinbun");
    Eq(RomajiConverter.ConvertFully("zutto"), "ずっと", "convertFully zutto");
    Eq(RomajiConverter.ConvertFully("kippu"), "きっぷ", "convertFully kippu");
    Eq(RomajiConverter.ConvertFully("wo"), "を", "romaji wo");
    Eq(RomajiConverter.ConvertFully("nn"), "ん", "romaji nn alone");
    Eq(RomajiConverter.ConvertFully("jikan"), "じかん", "convertFully jikan");
    Eq(RomajiConverter.ConvertFully("ryokou"), "りょこう", "convertFully ryokou");
    Eq(RomajiConverter.ConvertFully("denwa"), "でんわ", "convertFully denwa (n before w)");
    Eq(RomajiConverter.ConvertFully("genki"), "げんき", "convertFully genki");
    Eq(RomajiConverter.ConvertFully("hon"), "ほん", "convertFully hon");
    Eq(RomajiConverter.ConvertFully("xa"), "ぁ", "romaji xa small");
    Eq(RomajiConverter.ConvertFully("ko-hi-"), "こーひー", "romaji hyphen is choon");

    // ---------- KanaUtils ----------
    Eq(KanaUtils.ToKatakana("がっこう"), "ガッコウ", "toKatakana gakkou");
    Eq(KanaUtils.ToKatakana("ぁっゔヶ"), "ァッヴヶ", "toKatakana small/vu/ke");
    Eq(KanaUtils.ToHiragana("ガッコウ"), "がっこう", "toHiragana gakkou");
    Eq(KanaUtils.ToHiragana("ヴ"), "ゔ", "toHiragana vu");
    Eq(KanaUtils.ToKatakana("あー"), "アー", "toKatakana keeps choon");

    Check(KanaUtils.ToggleDakuten('か') == 'が', "toggle ka->ga");
    Check(KanaUtils.ToggleDakuten('が') == 'か', "toggle ga->ka");
    Check(KanaUtils.ToggleDakuten('は') == 'ば', "toggle ha->ba");
    Check(KanaUtils.ToggleDakuten('ば') == 'ぱ', "toggle ba->pa");
    Check(KanaUtils.ToggleDakuten('ぱ') == 'は', "toggle pa->ha");
    Check(KanaUtils.ToggleDakuten('つ') == 'づ', "toggle tsu->zu");
    Check(KanaUtils.ToggleDakuten('づ') == 'っ', "toggle zu->small tsu");
    Check(KanaUtils.ToggleDakuten('っ') == 'つ', "toggle small tsu->tsu");
    Check(KanaUtils.ToggleDakuten('あ') == 'ぁ', "toggle a->small a");
    Check(KanaUtils.ToggleDakuten('ぁ') == 'あ', "toggle small a->a");
    Check(KanaUtils.ToggleDakuten('う') == 'ゔ', "toggle u->vu");
    Check(KanaUtils.ToggleDakuten('ん') == 'ん', "toggle n unchanged");
    Check(KanaUtils.CanToggle('か') && !KanaUtils.CanToggle('ん'), "canToggle");

    Check(KanaUtils.ToggleRows.Length == 10, "toggle rows 10");
    Eq(KanaUtils.ToggleRows[8], "らりるれろ", "toggle row ra");
    Eq(KanaUtils.ToggleRows[9], "わをんー〜", "toggle row wa");
    Check(KanaUtils.RowIndexOf('き') == 1, "rowIndexOf ki");
    Check(KanaUtils.RowIndexOf('ん') == 9, "rowIndexOf n");
    Check(KanaUtils.RowIndexOf('x') == -1, "rowIndexOf none");
    Check(KanaUtils.NextInRow(0, 'あ') == 'い', "nextInRow a->i");
    Check(KanaUtils.NextInRow(0, 'お') == 'あ', "nextInRow o wraps to a");
    Check(KanaUtils.NextInRow(7, 'よ') == 'や', "nextInRow yo wraps to ya");
    Check(KanaUtils.NextInRow(9, '〜') == 'わ', "nextInRow wa row wraps");

    // ---------- Dictionary / ConversionEngine ----------
    var dict = new JapaneseDictionary();
    Check(dict.Size() >= 600, $"dictionary size >= 600 (actual={dict.Size()})");
    Check(dict.Size() == 3502, $"dictionary size == 3502 same as Android (actual={dict.Size()})");
    Check(dict.Lookup("がっこう").Contains("学校"), "dict gakkou -> 学校");
    Check(dict.Lookup("せんせい").Contains("先生"), "dict sensei -> 先生");
    Check(dict.Lookup("かいた").Contains("書いた"), "dict godan ta-form kaita");
    Check(dict.Lookup("かきます").Contains("書きます"), "dict godan masu-form");
    Check(dict.Lookup("たべた").Contains("食べた"), "dict ichidan ta-form");
    Check(dict.Lookup("たべます").Contains("食べます"), "dict ichidan masu-form");
    Check(dict.Lookup("たかかった").Contains("高かった"), "dict i-adj past");
    Check(dict.Lookup("しずかな").Contains("静かな"), "dict na-adj rentai");
    Check(dict.Lookup("いった").Contains("行った"), "dict iku exception itta");
    // 減る は五段動詞 (Android v1.2.1 の辞書修正を反映)
    Check(dict.Lookup("へった").Contains("減った"), "dict godan heru ta-form");
    Check(dict.Lookup("へらない").Contains("減らない"), "dict godan heru nai-form");
    Check(dict.Lookup("へれる").Contains("減れる"), "dict godan heru potential");
    Check(dict.Lookup("へろう").Contains("減ろう"), "dict godan heru volitional");
    Check(!dict.Lookup("へた").Contains("減た"), "dict heru not ichidan ta-form");
    Check(!dict.Lookup("へない").Contains("減ない"), "dict heru not ichidan nai-form");
    // 気持ちいい は「良い」の不規則活用を明示エントリで持つ (同上)
    Check(dict.Lookup("きもちいい").Contains("気持ちいい"), "dict kimochi-ii");
    Check(dict.Lookup("きもちよくない").Contains("気持ちよくない"), "dict kimochi-yokunai");
    Check(dict.Lookup("きもちよかった").Contains("気持ちよかった"), "dict kimochi-yokatta");
    Check(!dict.Has("きもちいくない"), "dict kimochi no regular i-adj generation");
    Check(dict.Lookup("き").Contains("木"), "dict single-kanji fallback ki");
    Check(dict.Lookup("そら").Contains("空"), "dict sora");
    Check(dict.Has("がっこう") && !dict.Has("ああああ"), "dict has");

    var learning = new MemoryLearningStore();
    var engine = new ConversionEngine(dict, learning);

    var cands = engine.Candidates("がっこう", "gakkou");
    Check(cands[0] == "学校", "engine first candidate is kanji");
    Check(cands.Contains("ガッコウ"), "engine has katakana candidate");
    Check(cands.Contains("がっこう"), "engine has hiragana candidate");
    Check(cands.Contains("gakkou"), "engine has romaji candidate");
    Check(cands.IndexOf("学校") < cands.IndexOf("ガッコウ")
        && cands.IndexOf("ガッコウ") < cands.IndexOf("がっこう")
        && cands.IndexOf("がっこう") < cands.IndexOf("gakkou"),
        "engine candidate order dict>katakana>hiragana>romaji");

    // 重複排除
    var dup = engine.Candidates("あめ", null);
    Check(dup.Distinct().Count() == dup.Count, "engine dedup");

    // 末尾助詞の簡易分離
    var split = engine.Candidates("がっこうは", null);
    Check(split[0] == "学校は", "engine particle split gakkou+ha");
    var split2 = engine.Candidates("ねこが", null);
    Check(split2[0] == "猫が", "engine particle split neko+ga");

    // 予測候補 (前方一致)
    var pred = engine.Predict("がっ", 8);
    Check(pred.Contains("学校"), "engine predict gakkou");
    Check(engine.Predict("", 8).Count == 0, "engine predict empty prefix");
    var entries = engine.PredictEntries("がっ", 8);
    Check(entries.Count > 0 && entries.Any(e => e[0] == "がっこう" && e[1] == "学校"),
        "engine predictEntries pair");

    // 学習: 頻度が候補順に反映される
    var before = engine.Candidates("はし", null);
    Check(before.IndexOf("橋") >= 0 && before.IndexOf("橋") < before.IndexOf("箸"),
        "engine hashi initial order");
    learning.RecordSelection("はし", "箸");
    learning.RecordSelection("はし", "箸");
    var after = engine.Candidates("はし", null);
    Check(after.IndexOf("箸") < after.IndexOf("橋"), "learning promotes selected candidate");
    Check(learning.GetFrequency("はし", "箸") == 2, "learning frequency count");
    learning.Clear();
    Check(learning.GetFrequency("はし", "箸") == 0, "learning clear");
    var restored = engine.Candidates("はし", null);
    Check(restored.IndexOf("橋") < restored.IndexOf("箸"), "learning clear restores order");

    // ---------- LearningFileStore (JSON永続化の往復) ----------
    string tmpDir = Path.Combine(Path.GetTempPath(), "yorukana-test-" + Guid.NewGuid().ToString("N"));
    string tmpFile = Path.Combine(tmpDir, "learning.json");
    var store1 = new LearningFileStore(tmpFile);
    store1.RecordSelection("がっこう", "学校");
    store1.RecordSelection("がっこう", "学校");
    store1.RecordSelection("ねこ", "猫");
    Check(File.Exists(tmpFile), "learning file written");
    var store2 = new LearningFileStore(tmpFile);
    Check(store2.GetFrequency("がっこう", "学校") == 2, "learning file roundtrip freq");
    Check(store2.GetFrequency("ねこ", "猫") == 1, "learning file roundtrip other reading");
    store2.Clear();
    var store3 = new LearningFileStore(tmpFile);
    Check(store3.GetFrequency("がっこう", "学校") == 0, "learning file clear persisted");
    try { Directory.Delete(tmpDir, recursive: true); } catch (Exception) { /* 後片付け失敗は無視 */ }

    // ---------- v1.1: xtsu / ltsu (ローマ字最長一致 4文字) ----------
    r = RomajiConverter.Convert("xtsu");
    Eq(r.Kana, "っ", "romaji xtsu (4-char longest match)");
    Eq(r.Pending, "", "romaji xtsu pending");
    r = RomajiConverter.Convert("ltsu");
    Eq(r.Kana, "っ", "romaji ltsu (4-char longest match)");
    Eq(RomajiConverter.ConvertFully("axtsu"), "あっ", "convertFully axtsu");

    // ---------- v1.1: INPUT 構造体のサイズ (x64 で 40 バイト) ----------
    Check(Marshal.SizeOf<NativeMethods.Input>() == 40,
        $"INPUT struct size == 40 on x64 (actual={Marshal.SizeOf<NativeMethods.Input>()})");
    Check(Marshal.SizeOf<NativeMethods.MouseInput>() == 32,
        $"MOUSEINPUT struct size == 32 on x64 (actual={Marshal.SizeOf<NativeMethods.MouseInput>()})");

    // ---------- v1.1: LearningFileStore の読み込み正規化 ----------
    string normDir = Path.Combine(Path.GetTempPath(), "yorukana-test-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(normDir);
    string normFile = Path.Combine(normDir, "learning.json");
    File.WriteAllText(normFile,
        "{\"はし\":{\"橋\":2,\"箸\":-5,\"\":9},\"ねこ\":null,\"\":{\"猫\":1},\"そら\":{\"空\":0}}");
    var normStore = new LearningFileStore(normFile);
    Check(normStore.GetFrequency("はし", "橋") == 2, "learning normalize keeps valid entry");
    Check(normStore.GetFrequency("はし", "箸") == 0, "learning normalize drops negative count");
    Check(normStore.GetFrequency("ねこ", "猫") == 0, "learning normalize drops null inner dict");
    Check(normStore.GetFrequency("", "猫") == 0, "learning normalize drops empty reading");
    Check(normStore.ReadingCount == 2, $"learning normalize reading count (actual={normStore.ReadingCount})");
    try { Directory.Delete(normDir, recursive: true); } catch (Exception) { /* 後片付け失敗は無視 */ }

    // ---------- v1.1: UserDictionary (純ロジック) ----------
    var ud = new UserDictionary();
    ud.Add("よるかな", "夜奏");
    ud.Add("よるかな", "夜奏"); // 重複は無視
    ud.Add("よるかな", "よるかなIME");
    ud.Add("", "空");          // 空の読みは無視
    ud.Add("たぶ", "タブ\t混入"); // タブは空白に置換される
    Check(ud.Size == 3, $"userdict size (actual={ud.Size})");
    Check(ud.Lookup("よるかな").Count == 2 && ud.Lookup("よるかな")[0] == "夜奏",
        "userdict lookup registration order");
    Check(ud.Lookup("たぶ")[0] == "タブ 混入", "userdict sanitize tab");
    Check(ud.Has("よるかな") && !ud.Has("ない"), "userdict has");
    Check(ud.Entries().Count == 3, "userdict entries count");
    Check(ud.Remove("よるかな", "夜奏"), "userdict remove returns true");
    Check(!ud.Remove("よるかな", "夜奏"), "userdict remove twice returns false");
    Check(ud.Lookup("よるかな").Count == 1 && ud.Lookup("よるかな")[0] == "よるかなIME",
        "userdict after remove");

    // ---------- v1.1: ユーザー辞書の優先順位 (学習より前) ----------
    var learning2 = new MemoryLearningStore();
    learning2.RecordSelection("はし", "橋");
    learning2.RecordSelection("はし", "橋");
    learning2.RecordSelection("はし", "橋");
    var ud2 = new UserDictionary();
    ud2.Add("はし", "お箸");
    var engine2 = new ConversionEngine(dict, learning2, ud2);
    var uc = engine2.Candidates("はし", null);
    Check(uc[0] == "お箸", "userdict candidate comes before learning-promoted");
    var uc2 = engine2.Candidates("よるかな", null);
    Check(uc2.Count > 0 && uc2[0] == "ヨルカナ", "no userdict entry falls back to katakana first");
    ud2.Add("よるかな", "夜奏");
    var uc3 = engine2.Candidates("よるかな", null);
    Check(uc3[0] == "夜奏", "userdict candidate first for non-dict reading");
    Check(uc3.IndexOf("夜奏") < uc3.IndexOf("ヨルカナ"), "userdict before katakana");

    // ---------- v1.1: UserDictionaryFileStore (JSON永続化の往復) ----------
    string udDir = Path.Combine(Path.GetTempPath(), "yorukana-test-" + Guid.NewGuid().ToString("N"));
    string udFile = Path.Combine(udDir, "userdict.json");
    var uds1 = new UserDictionaryFileStore(udFile);
    uds1.Add("せんせい", "先生(ユーザー)");
    uds1.Add("がっこう", "学校(ユーザー)");
    Check(File.Exists(udFile), "userdict file written");
    var uds2 = new UserDictionaryFileStore(udFile);
    Check(uds2.Dictionary.Lookup("せんせい").Contains("先生(ユーザー)"), "userdict file roundtrip");
    Check(uds2.Dictionary.Size == 2, "userdict file roundtrip size");
    Check(uds2.Remove("せんせい", "先生(ユーザー)"), "userdict file remove");
    var uds3 = new UserDictionaryFileStore(udFile);
    Check(uds3.Dictionary.Size == 1 && !uds3.Dictionary.Has("せんせい"), "userdict file remove persisted");
    try { Directory.Delete(udDir, recursive: true); } catch (Exception) { /* 後片付け失敗は無視 */ }

    Console.WriteLine($"ALL TESTS PASSED: {passed} checks");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine($"TESTS FAILED after {passed} checks");
    Environment.Exit(1);
}
