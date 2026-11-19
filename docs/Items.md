# アイテムの抽出と取り込み

本体のセットアップは `Build.ps1`、アイテムの抽出は `Extract-Items.ps1` で行います。
それぞれ独立して実行できます。Windows PowerShellで、ソースを展開したフォルダーから実行します。

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Extract-Items.ps1 -Compact "D:\媒体\AQUAZONE.ISO"
~~~

既定の出力先は `dist-items` です。出力先を変更する場合は `-OutputDirectory` に未使用のフォルダーを指定します。
ISOの代わりに、展開済みフォルダーやマウントしたCDのルートも指定できます。

## Compact版の出力

`Compact` フォルダーに次のファイルを配置します。

| フォルダー | ファイル数 | 抽出方法 |
| --- | --- | --- |
| Fish | 2 | CDのDNAから同梱FGW.EXEで各20尾を生成 |
| Tanks | 11 | セットアップ書庫の初期水槽1本とSHOPの無料水槽10本をコピー |
| Plants | 23 | 水槽に埋め込まれた種類・個体・画像を独立PLTに復元 |
| Food | 8 | 水槽に埋め込まれた餌の定義・画像を独立FDに復元 |
| Medicine | 12 | 水槽に埋め込まれた薬の定義・画像を独立MEDに復元 |
| Accessories | 19 | 水槽に埋め込まれた定義・画像を独立ACCに復元 |
| Gravel | 6 | 水槽と標準リソースの底砂・画像を独立GRVに復元 |
| Backgrounds | 10 | 水槽と標準リソースから640×480のBMPを取り出す |

計91ファイルです。同じ品目の状態・説明・画像の違いも含むファイル数です。
埋め込みアイテムは定義と画像の内容で重複を省き、元の個体情報を使って復元します。
`Inventory.csv` に復元元、種類ID、名称、出力ファイル名を記録します。
魚と水槽は上の表の方法で作成し、Inventory.csvの対象は埋め込みから復元したファイルです。
水草の個体IDは取り込み用に付け直します。元のISOや水槽ファイルは編集しません。

Compactのセットアップ書庫にあった `tetra.fsh` と `catfish.fsh` は100～112バイトの
ダミーファイルだったため、CD付属ツールとDNAから生成します。

Compact版の本体で、復元したアポノゲトン・スターチアヌスの個体一覧と、試験水槽への追加を確認しました。
試験水槽の水草個体数は12から13に増えました。全91ファイルの個別取り込みは未確認です。

## Classics版の出力

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Extract-Items.ps1 -Classics "D:\媒体\AZCLASSIC_BOX01.ISO"
~~~

`Classics` フォルダーに、水草15、餌4、薬4、アクセサリー77、底砂8、背景25の
計133ファイルを展開します。`-Compact` と `-Classics` を同時に指定すると、
Compact側91ファイルに加え、Classics側に互換変換した魚4ファイルも生成し、Classics側は計137ファイルになります。

### Classicsの魚4種をCompactで使う

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Extract-Items.ps1 -Compact "D:\媒体\AQUAZONE.ISO" -Classics "D:\媒体\AZCLASSIC_BOX01.ISO" -OutputDirectory ".\dist-both-items"
~~~

Compact CDのFGW.EXEを使うため、4種の生成には両方の媒体を指定します。
ゲーム本体をインストールする前にも実行できます。出力先の `Classics\Fish` に配置します。

| ファイル | 魚 |
| --- | --- |
| Arowana.fsh | シルバーアロワナ |
| Medaka.fsh | メダカ |
| M_Angel.fsh | マーブル・エンゼル |
| Pleco.fsh | ロイヤルプレコ |

各ファイル20尾分を生成します。取り込みで選んだ個体は元のFSHから水槽へ移動し、元ファイルの尾数が減ります。
生成した原本を保存し、作業用コピーから取り込んでください。

魚データの版番号をCompact向けに変換し、種類・飼育パラメーターと
生成した個体・履歴データを保持します。アロワナは収録画像の4コマを超える参照を動作表で補正します。
プレコはClassics CDの `GP23000.rez` から元の描画データを組み込みます。
元のDNA・ISOは編集しません。Windows 11上のCompact本体で4種それぞれの取り込み、
泳ぐ描画、保存した水槽の再読み込みを確認しました。長期飼育・繁殖は未検証です。


以前の検証では `grasss.plt`、`POWDER.FD`、青いサーフボードの取り込みを確認しました。
水草 `BACOPA.PLT` は個体一覧まで確認済みで、水槽への追加は未確認です。

## ゲームへの取り込み

水槽を開いてから、抽出先のCompactまたはClassics内のファイルを選びます。

- `Fish`、`Plants`、`Accessories`、`Gravel`、`Backgrounds` は［ファイル］→［ファイル取り込み］から選びます。魚と水草は個体一覧で選び、青い容器のアイコンから追加します。
- `Food` は［世話］→［餌］の左下の赤い下矢印から `.FD` を選びます。
- `Medicine` は［世話］→［薬品］で `.MED` を登録します。
- `Tanks` は［ファイル］→［水槽を開く］で選びます。水槽はプレイ中に更新されるため、原本を残す場合はコピーを開きます。


## Classics本体で使う魚について

両媒体を指定して生成する魚4種とAQUAZONE-Classics-Fish-Compact.zipはCompact向けです。Classics本体では別の形式の魚ファイルが必要です。今回、Compact向けメダカの取り込みは失敗し、Classicsの元の生成DLLによる正規形式のメダカは取り込めました。既存の抽出処理にはClassics形式の魚生成を追加していません。[Classicsの説明](Classics.md)を参照してください。
