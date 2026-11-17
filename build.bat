@echo off
chcp 65001 >nul
setlocal
rem ============================================================
rem  よるかな for Windows — ワンコマンドビルド
rem  使い方: build.bat を実行するだけ
rem    1) エンジン層テスト (src\YoruKanaTest) を実行
rem    2) 単体 exe を out\YoruKana.exe に publish
rem  必要なもの: .NET SDK 8
rem ============================================================
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [エラー] dotnet が見つかりません。.NET SDK 8 をインストールしてください。
    pause
    exit /b 1
)

echo === テストを実行します ===
dotnet run --project src\YoruKanaTest\YoruKanaTest.csproj -c Release
if errorlevel 1 (
    echo [エラー] テストが失敗しました。上の出力を確認してください。
    pause
    exit /b 1
)

echo.
echo === 単体 exe を publish します (win-x64 / 自己完結 / 単一ファイル) ===
dotnet publish src\YoruKanaWin\YoruKanaWin.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true -o out
if errorlevel 1 (
    echo [エラー] publish が失敗しました。上の出力を確認してください。
    pause
    exit /b 1
)

echo.
echo ビルド完了。成果物: %CD%\out\YoruKana.exe
pause
endlocal
