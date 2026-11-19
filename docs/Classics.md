# AQUAZONE Classics BOX01 Windows 11対応

更新日 2026年11月19日（日本時間）

日本語版Classics BOX01の媒体だけからゲーム本体と初期水槽を構築できます。Compactの媒体や、既存のゲームインストール先は参照しません。アイテム抽出は別の処理です。

## 構築する

Windows 11 x64、日本語の非Unicodeプログラム環境（CP932）、Windows PowerShell 5.1、Windows版7-Zipを使用します。公開用ソースを展開し、そのフォルダーでPowerShellを開きます。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Initialize-Tools.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1 -Classics "D:\媒体\AZCLASSIC_BOX01.ISO" -OutputDirectory ".\dist-classics"
```

ISOのパスを手元の場所へ置き換えます。展開済みCDフォルダー、マウントしたCDのルートも指定できます。出力先には未使用のフォルダーを指定します。

共通のInitialize-Tools.ps1は両版用のツールを準備します。Classics本体の構築で使う外部ツールは7-ZipとUnshieldです。Compact用のZig、unshieldv3、WineVDMはClassics本体の構築・起動には使いません。7-Zipの場所が標準と異なる場合は-SevenZip、準備済みツールの場所は-ToolsDirectoryで指定できます。

```text
dist-classics
├─ README.md
└─ AQUAZONE-Classics-Windows11
   ├─ Launch.cmd
   ├─ Launch.ps1
   ├─ Controls-Classics.xml
   ├─ AquaZone.exe
   ├─ Items\Starter.azn
   ├─ Runtime
   └─ System
```

本体の実行ファイルは媒体の原本を変更せず使用します。確認したファイルバージョンは1.08.0007、起動画面の表示はVer.1.8です。Classics用の画面部品の定義とリソースのパスをランチャーで設定し、終了時に設定を復元します。今回のClassics出力にはゲーム本体を収録します。Compact用の6本の起動ファイルとは構成が異なります。

## 起動して水槽を開く

1. AQUAZONE-Classics-Windows11全体を書き込み可能な場所へ置き、Launch.cmdをダブルクリックします。
2. ［ファイル］→［水槽を開く］でItems\Starter.aznを選びます。必要なら最初に水槽ファイルの作業用コピーを作ります。
3. ジャーナルの保存先を聞かれた場合は書き込み可能な場所を選びます。
4. 終了は［ファイル］→［終了］です。続きを遊ぶときは保存した水槽を開きます。

Classicsでは別の水槽を開く際も［水槽を開く］を使います。Compactの［水槽を閉じる］手順とは異なります。両版の本体や付属ツールは同時に起動せず、先に終了してから次を起動します。移動・コピーするときはRuntime、Systemを含むフォルダー全体を扱います。

## 製品登録と確認範囲

既存の製品登録情報があるWindows 11環境で、追加のシリアル入力画面なしに起動できました。クリーンな別PCでの初回登録は未確認です。この構築処理は製品登録を生成・変更せず、登録処理を回避しません。登録を要求される場合は、正規の製品情報と元媒体のセットアップを使ってください。

実機で次を確認しました。

- 元ISOから本体、システムリソース、ランタイム、初期水槽を抽出・配置。
- 起動と初期水槽の読み込み、水草・背景の描画。
- 元Classicsのfish_gen.dllで作成したClassics形式のメダカ1尾の取り込み。水槽の個体数が1尾になり、取り込み元の作業用FSHが20尾から19尾へ減少。
- メニューから通常終了し、再起動後に保存した水槽を読み込んで応答すること。

長期飼育、繁殖、全アイテムの取り込み、未登録PCでの動作は未確認です。

## アイテム抽出と魚ファイルの形式

アイテム抽出は本体の構築と分けて実行できます。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Extract-Items.ps1 -Classics "D:\媒体\AZCLASSIC_BOX01.ISO" -OutputDirectory ".\dist-classics-items"
```

既存のClassics単独抽出は133ファイルを取得します。今回の変更ではこの抽出仕様・件数を変更していません。詳細は公開用ソースのdocs/Items.mdを参照してください。

**AQUAZONE-Classics-Fish-Compact.zipと、両媒体を指定した抽出処理で生成する魚4種は、Compact向けです。Classics本体での利用を保証しません。** Compact向けメダカはClassicsでプレビューできましたが、取り込みは「リソースを見つけられませんでした」で失敗しました。

実機検証用には、Classics媒体の元のfish_gen.dllと魚DNAからClassics形式のメダカを作成し、取り込みに成功しました。Classics形式には魚個体ごとのFsI2など、既存のCompact向け生成処理にはないレコードが含まれます。版番号の書き換えだけでは対応できません。この検証用の生成器を公開用の抽出処理へ組み込む変更は行っていません。Classicsで使う魚には、同版で利用できる正規形式のデータを使用してください。
