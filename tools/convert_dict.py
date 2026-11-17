#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""DictionaryData.java (Android版) -> DictionaryData.cs (Windows版) 機械変換スクリプト。

Android版 ~/workspace/yorukana-ime の DictionaryData.java から
ENTRIES / GODAN / ICHIDAN / I_ADJ / NA_ADJ / SINGLE_KANJI の文字列配列を抽出し、
C# の static readonly string[] として src/YoruKanaWin/DictionaryData.cs を生成する。
Java 側の文字列リテラルにはエスケープが含まれないことを前提に検証し、
C# 側では " と \\ をエスケープして出力する。

使い方: python3 tools/convert_dict.py [DictionaryData.java のパス]
"""
import re
import sys
from pathlib import Path

ARRAYS = ["ENTRIES", "GODAN", "ICHIDAN", "I_ADJ", "NA_ADJ", "SINGLE_KANJI"]

src_path = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(
    "~/workspace/yorukana-ime/app/src/main/java/jp/yorukana/ime/DictionaryData.java"
).expanduser()
src = src_path.read_text(encoding="utf-8")

if "\\" in src:
    raise SystemExit("DictionaryData.java にバックスラッシュが含まれます。エスケープ処理を見直してください。")

out = []
out.append("namespace YoruKanaWin;")
out.append("")
out.append("// ===== 内蔵辞書データ (Android版 DictionaryData.java から機械変換) =====")
out.append("// 生成元: yorukana-ime/app/src/main/java/jp/yorukana/ime/DictionaryData.java")
out.append("// 生成方法: tools/convert_dict.py で文字列配列を抽出して C# 形式に変換した。")
out.append("// ENTRIES: \"読み=候補1,候補2,...\" / GODAN・ICHIDAN・I_ADJ・NA_ADJ: \"読み|漢字形\"")
out.append("// SINGLE_KANJI: 1文字読みに対する単漢字フォールバック。")
out.append("internal static class DictionaryData")
out.append("{")

total = 0
for name in ARRAYS:
    m = re.search(name + r"\s*=\s*\{(.*?)\n    \};", src, re.S)
    if not m:
        raise SystemExit(f"配列 {name} が見つかりません")
    strs = re.findall(r'"([^"]*)"', m.group(1))
    total += len(strs)
    out.append(f"    internal static readonly string[] {name} =")
    out.append("    {")
    for s in strs:
        esc = s.replace("\\", "\\\\").replace('"', '\\"')
        out.append(f'        "{esc}",')
    out.append("    };")
    out.append("")
    print(f"{name}: {len(strs)} 件")

out.append("}")
dest = Path(__file__).resolve().parent.parent / "src" / "YoruKanaWin" / "DictionaryData.cs"
dest.write_text("\n".join(out), encoding="utf-8")
print(f"合計 {total} 件 -> {dest}")
