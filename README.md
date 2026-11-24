# 🌙 よるかな for Windows

> 📦 **このリポジトリは [i-mabu/yorukana-ime](https://github.com/i-mabu/yorukana-ime) に統合されました。** 今後の開発は yorukana-ime の `windows-companion/` フォルダで行います。このリポジトリはアーカイブされます。

Android 用オリジナル日本語IME「よるかな IME」の Windows 版です。C# / .NET 8 (WinForms) で実装しました。
変換エンジンと辞書は Android 版から忠実に移植しており、辞書の読み数は Android 版と完全に一致します（3,408 読み）。

- 対応: Windows 10 / 11 (x64)
- バージョン: 1.1.1

## ⚠️ 方式について（最初にお読みください）

**本アプリは Windows のシステム IME（TSF）ではありません。**
本物のシステム IME の開発には TSF (Text Services Framework) のネイティブ開発が必要で、本プロジェクトの開発環境 (Linux VM) ではビルドできません。そのため、次の「入力コンパニオン」方式を採っています。

1. タスクトレイに常駐します
2. どのアプリを使っているときでも、グローバルホットキー **Ctrl+Alt+J** で小さな入力ウィンドウを呼び出します
3. ウィンドウ内でローマ字入力→変換し、**Enter** で確定すると、直前に使っていたアプリへテキストを送り込みます

テキストの送信は、対象ウィンドウを前面に戻したうえで「クリップボード経由の Ctrl+V 貼り付け」を基本とします（元のクリップボード内容は保存され、成功・失敗どちらの場合も復元されます）。送信前に対象ウィンドウの生存と前面化を確認し、確認できない場合は誤送信防止のため送信を中止します（その際、文章はクリップボードに残して通知します）。クリップボードが使えない場合は SendInput の Unicode 入力にフォールバックします。

## 📋 設計原則（手動型）

yorukana は「自動で変換しない・自動で判定しない」手動型の IME です。変換は入力ウィンドウ内の Space、元のアプリへの送信は Enter の明示操作だけです。詳しくは [MANUAL-INPUT-DESIGN.md](MANUAL-INPUT-DESIGN.md) を参照してください。

## ✨ 機能

- **ローマ字入力→変換**: Android 版と同一の変換エンジン。促音・撥音・拗音・外来音・未確定ローマ字 (pending) の扱いまで同じです
- **変換候補**: 候補バーに辞書候補（学習頻度順）→カタカナ→ひらがな→ローマ字の順で表示。前方一致の予測候補も続きます。末尾助詞（は・が・を等）の簡易分離変換にも対応
- **内蔵辞書 3,408 読み**: 明示エントリに、五段/一段動詞・い形容詞・な形容詞の活用形を生成登録。単漢字フォールバック付き
- **学習機能**: 確定した候補の頻度を `%AppData%\YoruKana\learning.json` に保存し、次回から候補の上位に表示します。トレイメニューからリセット可能
- **ユーザー辞書**（v1.1〜、Android版と機能水準を統一）: トレイメニューの「ユーザー辞書を編集...」から よみ＋単語 を登録でき、変換候補の**最優先**（学習より前）に表示されます。`%AppData%\YoruKana\userdict.json` に保存されます
- **英数モード**: Alt+A（または左上のモード表示クリック）で あ/A を切替。英数モードでは打った文字がそのまま蓄えられ、Enter で送信されます
- **トレイ常駐**: メニュー（入力ウィンドウを開く／学習リセット／終了）、ダブルクリックでも開けます。二重起動防止（Mutex）付き
- **夜テーマ**: 入力ウィンドウは濃紺の夜テーマ固定です

## 📖 使い方

1. `YoruKana.exe` をダブルクリックして起動します（**自己完結型の単体 exe のため、.NET ランタイムのインストールは不要です**）。タスクトレイに「よ」アイコンが出れば常駐中です
2. テキストを入力したいアプリ（メモ帳、ブラウザ等）をクリックして前面にします
3. **Ctrl+Alt+J** を押すと入力ウィンドウが開きます
4. ローマ字で入力します（例: `gakkou` と打つと「がっこう」と表示されます）
5. **Space** で変換候補を順に選びます。候補をクリックすると、その候補で確定して入力を続けられます
6. **Enter** で全文を元のアプリへ送信します。**Esc** でキャンセルします
7. `,` は「、」、`.` は「。」として入力されます。文の途中で候補クリックや句読点で区切ると、確定済みの文が左側に蓄えられていきます

### 📝 キー操作

| キー                 | 動作                                       |
| -------------------- | ------------------------------------------ |
| 文字キー (a-z, -, ') | ローマ字入力（英数モードではそのまま入力） |
| Space                | 変換開始／次の候補へ（英数モードでは空白） |
| Enter                | 確定して元のアプリへ送信                   |
| Esc                  | キャンセル（送信しない）                   |
| Backspace            | 変換中は変換を解除、それ以外は1文字削除    |
| Alt+A                | あ（日本語）/ A（英数）モード切替          |
| 候補クリック         | その候補で確定し、入力を継続               |

## 🚀 ビルド方法

必要なもの: .NET SDK 8

ワンコマンドビルド (テスト → 単体 exe の publish までを一括実行し、
成果物のパスを表示します):

```powershell
# Windows
build.bat
```

```bash
# Linux (クロスビルド)
./build.sh
```

手でコマンドを打つ場合は以下です。

```powershell
# テスト (エンジン層の単体テスト、コンソール)
dotnet run --project src/YoruKanaTest/YoruKanaTest.csproj -c Release

# 単体 exe の生成 (win-x64 / 自己完結 / 単一ファイル / 圧縮)
dotnet publish src/YoruKanaWin/YoruKanaWin.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true -o out
# → out/YoruKana.exe
```

Linux からもクロスビルドできます（`EnableWindowsTargeting` を有効化済み）。
この VM でビルドする場合は、NuGet の取得がプロキシで止まるため、ローカルフィード
`~/workspace/tools/nuget-local` を `-p:RestoreSources=...` で指定し、`TMPDIR` を
ワークスペース配下に逃がしてください（`~/TOOLS.md` の .NET SDK 節を参照）。

### 📁 辞書データの生成

`src/YoruKanaWin/DictionaryData.cs` は Android 版の `DictionaryData.java` から
機械変換で生成しています（手で書き写したものではありません）。

```bash
python3 tools/convert_dict.py   # yorukana-ime の DictionaryData.java を読み、C# 形式で出力
```

## 📁 構成

| パス                                   | 内容                                                                                                                    |
| -------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `src/YoruKanaWin/Engine.cs`            | 変換エンジン（GUI非依存。Android版 RomajiConverter / KanaUtils / Dictionary / ConversionEngine / LearningStore の移植） |
| `src/YoruKanaWin/DictionaryData.cs`    | 辞書データ（`tools/convert_dict.py` で Java 版から機械変換）                                                            |
| `src/YoruKanaWin/LearningFileStore.cs` | 学習の JSON 永続化（%AppData%\YoruKana\learning.json、読み込み時に不正データを正規化）                                  |
| `src/YoruKanaWin/UserDictionary.cs`    | ユーザー辞書（純ロジック＋JSON 永続化 %AppData%\YoruKana\userdict.json）                                                |
| `src/YoruKanaWin/UserDictForm.cs`      | ユーザー辞書の編集フォーム（追加・一覧・削除）                                                                          |
| `src/YoruKanaWin/InputForm.cs`         | 入力ウィンドウ（composing 表示・候補バー・キー処理）                                                                    |
| `src/YoruKanaWin/TextSender.cs`        | テキスト送信（前面復帰→クリップボード貼り付け／SendInput フォールバック）                                               |
| `src/YoruKanaWin/NativeMethods.cs`     | Win32 P/Invoke（RegisterHotKey / SetForegroundWindow / SendInput）                                                      |
| `src/YoruKanaWin/Program.cs`           | エントリポイント、トレイ常駐、ホットキー受信、二重起動防止                                                              |
| `src/YoruKanaTest/`                    | エンジン層の単体テスト（Android版 TestMain.java の移植＋永続化テスト）                                                  |

## 📋 制限事項（正直なところ）

- **Windows 実機での動作は未検証です**（開発は Linux 上で行い、クロスビルドで exe を生成しています。この VM では GUI 動作確認ができません。エンジン層のテスト 143 件は Linux 上で全件パス済み）
- 予期しないエラーは `%AppData%\YoruKana\error.log` に記録され、トレイのバルーンで通知されます（UIスレッドの例外では動作を継続します）
- システム IME ではないため、文字入力欄ごとに有効化するのではなく、ホットキーで都度呼び出す方式です。入力ウィンドウ表示中は、元のアプリの入力欄に直接文字は入ります（送信は Enter 時の一括）
- **管理者権限で実行中のアプリへは送信できない場合があります**（Windows の UIPI による制限。その場合は本アプリも管理者権限で起動してください）
- 貼り付け（Ctrl+V）を受け付けないアプリ（一部のゲーム、パスワード欄、リモートデスクトップ等）では、フォールバックの SendInput でも入力できないことがあります
- クリップボードの復元は貼り付け後 約250ms 待ってから行いますが、その間に他アプリがクリップボードを変更した場合は元内容で上書きされます
- カーソル移動の概念はありません（←→キーは無効です）。修正は Backspace で後ろから消す形になります
- 文節変換は Android 版と同じく簡易版です（全体変換＋末尾助詞の分離のみ。複数文節の解析はしません）
- exe は未署名のため、初回起動時に SmartScreen 警告（「Windows によって PC が保護されました」）が出る場合があります。実行する場合は「詳細情報」→「実行」を選んでください
- ホットキー Ctrl+Alt+J が他アプリと競合して登録できない場合は、起動時にバルーンで知らせます（トレイメニューからは開けます。ホットキーの変更機能は未実装です）

バージョン履歴は [CHANGELOG.md](CHANGELOG.md) を参照してください。
