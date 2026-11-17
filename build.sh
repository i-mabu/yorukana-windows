#!/usr/bin/env bash
# ============================================================
#  よるかな for Windows — ワンコマンドビルド (Linux クロスビルド用)
#  使い方: ./build.sh
#    1) エンジン層テスト (src/YoruKanaTest) を実行
#    2) 単体 exe を out/YoruKana.exe に publish
#  必要なもの: .NET SDK 8
#  Windows では build.bat を使ってください。
# ============================================================
set -euo pipefail
cd "$(dirname "$0")"

# --- dotnet を探す (PATH → この VM のローカル SDK の順) ---
if command -v dotnet >/dev/null 2>&1; then
  DOTNET=dotnet
elif [ -x "$HOME/workspace/tools/dotnet/dotnet" ]; then
  DOTNET="$HOME/workspace/tools/dotnet/dotnet"
else
  echo "[エラー] dotnet が見つかりません。.NET SDK 8 をインストールしてください。" >&2
  exit 1
fi

# --- この VM 向けの回避策 (~/TOOLS.md の .NET SDK 節) ---
# /tmp は小さな tmpfs なので TMPDIR をワークスペース配下へ逃がす。
# NuGet の直接取得はプロキシで止まるため、ローカルフィードがあれば使う。
export TMPDIR="${TMPDIR:-$HOME/workspace/tmp-dotnet}"
mkdir -p "$TMPDIR"
RESTORE_OPT=""
if [ -d "$HOME/workspace/tools/nuget-local" ]; then
  RESTORE_OPT="-p:RestoreSources=$HOME/workspace/tools/nuget-local"
fi

echo "=== テストを実行します ==="
# shellcheck disable=SC2086
"$DOTNET" run --project src/YoruKanaTest/YoruKanaTest.csproj -c Release $RESTORE_OPT

echo
echo "=== 単体 exe を publish します (win-x64 / 自己完結 / 単一ファイル) ==="
# shellcheck disable=SC2086
"$DOTNET" publish src/YoruKanaWin/YoruKanaWin.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -o out $RESTORE_OPT

echo
echo "ビルド完了。成果物: $(pwd)/out/YoruKana.exe"
