# AQUAZONE Item Extractor

[GitHub: fcnt/AQUAZONE_Item_Extractor](https://github.com/fcnt/AQUAZONE_Item_Extractor) · [GNU General Public License v3.0](LICENSE)

日本語版 **AQUAZONE Compact 1.5 / Classics BOX01** の手元の媒体から、魚・水槽・水草・餌・薬・アクセサリー・底砂・背景を抽出・復元する非公式ツールです。

共通の `Extract-Items.ps1` でCompactのみ・Classicsのみ・両方の抽出に対応します。ゲーム本体のインストール先は参照しません。本体の抽出・Windows 11向け配置には、別プロジェクト [AQUAZONE Win11](https://github.com/fcnt/AQUAZONE_Win11) を使用します。

## 抽出できるもの

| 指定する媒体 | 出力先の構成 | アイテムファイル数 |
| --- | --- | ---: |
| Compactのみ | `Compact/` | 91 |
| Classicsのみ | `Classics/` | 133 |
| 両方 | `Compact/` と `Classics/` | 91 + 137 |

件数は同じ品目の状態・説明・画像違いを含むファイル数です。`Inventory.csv` と手順書はこの件数に含めません。

| 種類 | Compact | Classics単独 |
| --- | ---: | ---: |
| 魚 | 2 | 0 |
| 水槽 | 11 | 0 |
| 水草 | 23 | 15 |
| 餌 | 8 | 4 |
| 薬 | 12 | 4 |
| アクセサリー | 19 | 77 |
| 底砂 | 6 | 8 |
| 背景 | 10 | 25 |
| **合計** | **91** | **133** |

両媒体を指定すると、Classics側へシルバーアロワナ、メダカ、マーブル・エンゼル、ロイヤルプレコの4ファイルを各20尾分追加生成します。

**この4種の生成物はCompact向けです。Classics本体向けの魚生成には対応していません。** 生成にはCompact媒体の `FGW.EXE` が必要なため、Classics単独では魚4種を生成しません。Classics単独の水槽抽出も現在の処理の対象外です。

## 必要なもの

- Windows 11 x64。
- 日本語の非Unicodeプログラム環境（CP932）。Windowsの「UTF-8を使用」のベータ設定は無効。
- Windows PowerShell 5.1。
- Windows版 [7-Zip](https://www.7-zip.org/)。
- 抽出する版の正規媒体。
- 初回のツール取得用のインターネット接続と、数GBの空き容量。

ゲーム本体のインストールやProgram Extractorの配置は不要です。WSL・Pythonも不要です。確認対象は上記の日本語版媒体で、別の版・言語の媒体は未検証です。

## 使い方

リポジトリをダウンロードまたはクローンし、`Extract-Items.ps1` のあるフォルダーでWindows PowerShellを開きます。補助コードと `docs` の構成を維持してください。

### 1. ツールを準備する

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Initialize-Tools.ps1
```

必要なWindows用ツールを取得し、ダウンロードしたアーカイブのSHA-256を照合して `.tools` に配置します。準備済みの取得物は再利用します。共通の準備スクリプトを使うため、プログラム抽出向けのZig・WineVDMなども取得します。

### 2. 媒体から抽出する

ISOのパスを手元の場所に置き換えてください。ISOの代わりに展開済みCDフォルダーやマウントしたCDのルートも指定できます。

**Compactのみ**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Extract-Items.ps1 -Compact "D:\媒体\AQUAZONE.ISO" -OutputDirectory ".\dist-items\compact"
```

**Classicsのみ**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Extract-Items.ps1 -Classics "D:\媒体\AZCLASSIC_BOX01.ISO" -OutputDirectory ".\dist-items\classics"
```

**両方をまとめて抽出**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Extract-Items.ps1 -Compact "D:\媒体\AQUAZONE.ISO" -Classics "D:\媒体\AZCLASSIC_BOX01.ISO" -OutputDirectory ".\dist-items\both"
```

出力先には未使用のフォルダーを指定します。`-OutputDirectory` を省略すると、スクリプトと同じ場所の `dist-items` を使います。

両方を指定した場合の出力例：

```text
dist-items/both/
├─ README.md
├─ Compact/
│  ├─ Fish/
│  ├─ Tanks/
│  ├─ Plants/
│  ├─ Food/
│  ├─ Medicine/
│  ├─ Accessories/
│  ├─ Gravel/
│  ├─ Backgrounds/
│  └─ Inventory.csv
└─ Classics/
   ├─ Fish/
   ├─ Plants/
   ├─ Food/
   ├─ Medicine/
   ├─ Accessories/
   ├─ Gravel/
   └─ Backgrounds/
```

`Inventory.csv` はCompactの埋め込みアイテム78件について、名称・出典・復元ファイル名を記録します。魚と水槽は対象外です。

## ゲームで使う

| データ | 操作 |
| --- | --- |
| 魚・水草・アクセサリー・底砂・背景 | 水槽を開き、［ファイル］→［ファイル取り込み］から種類を選ぶ |
| 水槽 `.azn` | ［ファイル］→［水槽を開く］ |
| 餌 `.fd` | ［世話］→［餌］から新規登録 |
| 薬 `.med` | ［世話］→［薬品］から新規登録 |

魚・水草は個体一覧で選択し、下部の青い容器のアイコンから水槽へ移します。魚の取り込みでは個体が元のFSHから水槽へ移り、元ファイルの尾数が減ります。魚・水槽の原本を残し、作業用コピーを使ってください。

全アイテムが両版本体で利用できることは確認していません。特にCompact向けの魚4種をClassics本体へ取り込む用途は対象外です。

## オプション

| オプション | 用途 |
| --- | --- |
| `-Compact` | CompactのISOまたはCDフォルダー |
| `-Classics` | ClassicsのISOまたはCDフォルダー。`-Compact` と併用可能 |
| `-OutputDirectory` | 新規の出力先フォルダー |
| `-ToolsDirectory` | 準備済みツールのフォルダー。既定は `.tools` |
| `-SevenZip` | 標準の場所以外にある `7z.exe` のフルパス |

`-SevenZip` と `-ToolsDirectory` は `Initialize-Tools.ps1` でも指定できます。

## 確認済みの範囲と制限

- Windows PowerShell 5.1で、両版の元ISOからの抽出を確認。
- 両媒体指定でCompact側91、Classics側137ファイルを取得し、生成した魚6ファイルがそれぞれ20尾を持つことを確認。
- Compact本体で、復元した水草の取り込みと、Classics由来の魚4種それぞれの取り込み・泳ぐ描画・保存後の再読み込みを確認。
- 全アイテムの個別取り込み、長期飼育・繁殖は未検証。
- Classics本体向け魚ファイルの生成は未実装。Compact向けメダカはClassicsでプレビューできても取り込みに失敗。

元ISO・DNA・水槽原本を抽出処理で編集しません。魚の生成には媒体に収録された元の生成ツールを使用します。

## ドキュメント

- [抽出方法・ファイルの仕様・取り込み手順](docs/Items.md)
- [Classicsの魚形式と検証結果](docs/Classics.md)

`docs/Classics.md` にある `Build.ps1` は別プロジェクトProgram Extractorのスクリプトです。

## 外部ツール

- [7-Zip](https://www.7-zip.org/)
- [Zig](https://ziglang.org/download/0.13.0/release-notes.html)
- [unshieldv3](https://github.com/wfr/unshieldv3)
- [Unshield](https://github.com/twogood/unshield) / [Windows版の配布定義](https://github.com/ScoopInstaller/Main/blob/master/bucket/unshield.json)
- [WineVDM 0.9.0](https://github.com/otya128/winevdm/releases/tag/v0.9.0) — 共通のツール準備処理で取得

## 権利・ライセンス

このリポジトリは非公式です。元ISO、ゲームの実行ファイル・素材、製品登録情報、セーブデータは含みません。ゲーム・素材・外部ツールの権利は各権利者に帰属し、外部ツールには各配布元のライセンスが適用されます。

Copyright (c) 2026 fcnt

本リポジトリの自作コード・文書は [GNU General Public License v3.0](LICENSE) （SPDX: `GPL-3.0-only`）で公開します。詳細はLICENSEファイルを参照してください。このライセンスは元ゲームのプログラム・素材や外部ツールには適用されません。
