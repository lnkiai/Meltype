# Meltype

**雪解けのように、半角/全角の壁を溶かす日本語入力。**

> このリポジトリは [Meltype](https://github.com/yksr-melt/Meltype) のフォークです。Windows で、打った文字を入力欄に直接 (下線付きで) 入れる Meltype IME (TSF) を足しています ([設計](docs/TSF_DESIGN.md)・[使い方](docs/USAGE.md#meltype-ime))。
> Meltype IME は本家のリリースの zip には入っていないので、このリポジトリの [Releases](https://github.com/lnkiai/Meltype/releases) の zip か、ソースから `Install-Meltype.ps1` で入れてください (下の「インストール」)。不具合・脆弱性の報告は、このリポジトリへお願いします。
> 2026 年に lnkiai が変更しています (本家のファイルを変えたところも含みます)。

半角/全角 キーを押さなくても、日本語と英語を打ち分けられるようにする Windows 常駐ツールです。
(開発中は AutoIME という仮の名前でした。以前の設定と学習データは、Meltype の初回起動時に自動で引き継ぎます)

Windows 版のほか、Mac 版・Linux 版のプレビュー版があります ([mac/README.md](mac/README.md)。Linux 版は IBus のエンジン)。プレビュー版は、まだ一部の機能が無く、動きも変わることがあります。

## できること

- 半角/全角 キーを押さずに、ローマ字のまま日本語と英語を混ぜて打てます (`kyouhagoogledekensaku` → 今日はgoogleで検索)
- 日本語は変換ボックスでかな・漢字に変換し、英単語 (`google` `github` `hello` …) は自動で英字のまま
- 英文 (`I want to go to the park`) も、そのまま打てます
- 絵文字・顔文字の変換 (えがお → 😊)、よくある書き間違いの指摘 (ブレスレッド → ブレスレット)
- VS Code やターミナルでは基本は英数、コメントや文字列の中だけ日本語
- 打った内容をネットワークに送りません。判定・変換はすべて PC の中で行います

## インストール

このフォーク (Meltype IME を使う) は、[Releases](https://github.com/lnkiai/Meltype/releases) の `Meltype-IME-<版>-setup.exe` をダブルクリックすると入ります
(`Meltype-IME-<版>-windows.zip` を展開して `Install.cmd` をダブルクリックしても同じです)。
途中で「Meltype IME を Windows に登録します」と出て管理者権限の確認が出るので、「はい」を選んでください。入ったら **Win + Space** で「Meltype」を選びます。
コード署名をしていないので、「Windows によって PC が保護されました」と出たら「詳細情報」→「実行」で入れられます。アンインストールは下の「本家のリリースで入れた場合」と同じです。

ソースから入れるときは、.NET 10 SDK と、Meltype IME をビルドする Visual Studio Build Tools の「C++ によるデスクトップ開発」が要ります。

```powershell
git clone https://github.com/lnkiai/Meltype
cd Meltype
powershell -ExecutionPolicy Bypass -File .\Install-Meltype.ps1              # ビルドして入れる (Meltype IME の登録で管理者権限の確認が出ます)
powershell -ExecutionPolicy Bypass -File .\Uninstall-Meltype.ps1 -RemoveData # アンインストール (Meltype IME の登録も外し、設定と学習データも消す)
```

Meltype IME を入れずに使うときは `Install-Meltype.ps1 -NoIme` (管理者権限は要りません)。ビルドしたものはソースのフォルダーの `app-build` に置かれ、アンインストールしても残ります (要らなければ消してください)。

以下は本家のリリース (Meltype IME は入っていません) の入れ方です。

1. [Releases](https://github.com/yksr-melt/Meltype/releases) から `Meltype-<version>-windows.zip` をダウンロードして展開する
   (Mac 版は `Meltype-<version>-mac.zip`、Linux 版は `Meltype-<version>-linux.zip`。どちらもプレビュー版)
2. `Install.cmd` をダブルクリックする (管理者権限は不要)
   - Meltype はコード署名をしていないので、「Windows によって PC が保護されました」と出ることがあります。「詳細情報」→「実行」で入れられます。
   - キーボードの入力を受け持つソフトなので、ウイルス対策ソフトが誤って止めることがあります。そのときは、お使いのウイルス対策ソフトで Meltype のフォルダーを許可してください。
   - ダウンロードした zip が本物か確かめたいときは、リリースのページに出ている SHA-256 と比べてください (PowerShell: `Get-FileHash .\Meltype-<version>-windows.zip`)。
3. タスクトレイに「あ」のアイコンが出れば動いています。Windows の起動時にも自動で起動します。

このフォークは自動では更新しません (本家のリリースには Meltype IME が入っていないため、本家の版に更新しないようにしています)。新しい版は、このリポジトリの Releases の zip か、ソースから入れ直してください。
本家のリリース・このリポジトリの Releases の zip で入れた場合のアンインストールは、トレイの Meltype のアイコンを右クリック →「アンインストール...」か、Windows の「設定」→「アプリ」→「インストールされているアプリ」で Meltype の「…」→「アンインストール」を選びます (設定と学習データも消えます)。zip の中の `Uninstall.cmd` をダブルクリックしても同じです。

必要なもの: Windows 10 / 11 (64bit)、Microsoft IME (Windows 標準の日本語入力)。.NET は同梱しているので、別に入れる必要はありません。

## 使い始める

メモ帳やブラウザーの入力欄で、IME を気にせずそのままローマ字で打ってください。

- インストールで Meltype IME を入れた場合は、**Win + Space** で「Meltype」を選びます。打った文字は入力欄にそのまま下線付きで入り、変換の候補は入力位置の下に一覧で出ます ([使い方](docs/USAGE.md#meltype-ime))
- Meltype IME を入れていない場合は、日本語はかなで、英単語は英字のまま、カーソルの下の変換ボックスに出ます
- **Enter** で確定、**Space** で漢字に変換 (英単語のときは確定して空白)
- 変換中は ← → で文節を選び、Space / ↓ で候補を切り替え
- **F7** でカタカナ、**F10** で英字。英字にして確定した語は、次から英字になります
- **半角/全角** で英数 (そのまま入力) ⇔ 日本語、**Ctrl + 半角/全角** で Meltype 自体の一時停止 / 再開
- よく使う言葉は、トレイのアイコンを右クリック →「ユーザー辞書...」で登録できます

詳しい使い方 (キー操作・判定の強さ・かな入力・コードエディター・設定など) は [docs/USAGE.md](docs/USAGE.md) にあります。

## よくある質問

**タスクバーの IME の表示がずっと「A」のまま**
Meltype が Windows の IME を OFF にして、代わりに入力を受け持っているためです (故障ではありません)。今のモードは、入力欄に入ったときにカーソルの近くに出る「あ」「A」か、タスクトレイの Meltype のアイコンで分かります。

**Google 日本語入力など、ほかの IME も使いたい**
Ctrl + 半角/全角 で Meltype を一時停止してから使ってください。

**英語のつもりがかなになった / かなのつもりが英字になった**
F10 (英字) / F6 (ひらがな) で直して確定すると、次からその語は直した方になります。トレイの右クリック →「自動判定の強さ」でも調整できます。

**おかしな動きを見つけた**
トレイのアイコンを右クリック →「不具合の報告・提案...」から報告できます。「どのアプリで」「何と打って」「どうなったか」を書いてもらえると助かります。

## プライバシー

Meltype はキーボードの入力を監視して動くツールですが、打った内容をネットワークに送ることはありません。 セキュリティの方針と脆弱性の報告先は [SECURITY.md](SECURITY.md)。通信するのは、自動更新で GitHub に新しい版があるかを確かめるとき (送るのは今の版だけ。このフォークは自動更新をしないので確かめません) と、自分で開いた不具合報告のフォームだけです。
保存するのは `%LOCALAPPDATA%\Meltype` の設定・学習データ・ユーザー辞書と、ファイルログを ON にしたときのログだけです。

## ライセンス

Meltype は **GNU General Public License v3.0** ([LICENSE](LICENSE)) で公開しています。

- 個人・会社でそのまま使う、GPL v3 の条件 (改造版もソースを公開) で改造・再配布する → 無料で自由に使えます
- GPL v3 の条件で使えない場合 (製品に組み込んでソースを公開せずに配布したいなど、非公開で利用したい場合) は、メールでご相談ください: ibutya0319@gmail.com (本家の作者)
- このフォークで足した Meltype IME の部分 (`native/tip/`、`src/Meltype/Tip/` など、先頭に `Copyright (C) 2026 lnkiai` とあるファイル) は lnkiai の著作物で、GPL v3 (またはそれ以降) でだけ提供しています

貢献の方法と貢献者ライセンス同意 (CLA) は [CONTRIBUTING.md](CONTRIBUTING.md) を参照してください。

ソースファイルの先頭には `SPDX-License-Identifier: GPL-3.0-or-later` を付けています。配布用パッケージに同梱している .NET ランタイム (MIT ライセンス) と、実行時に使う Windows の機能は [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) を参照してください。アプリのバージョン・著作権・ライセンスは、トレイの「Meltype について...」で確認できます。

```
Meltype
Copyright (C) 2026 雪代 / Yukishiro (@yksr_melt / @yksr-melt)

This program is free software: you can redistribute it and/or modify it under the terms of the
GNU General Public License as published by the Free Software Foundation, either version 3 of the
License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
General Public License for more details.
```

## 協力してくださった方々

テスト版を使って、不具合の報告や意見をくださった方々です。ありがとうございました (敬称略)。

- くらいど！ ([@Kuraido8888](https://x.com/Kuraido8888))
- しぐれ ([@Akisameee0465](https://x.com/Akisameee0465))
- 琴音Link
- あげちゃ
- うな ([@una08142009](https://x.com/una08142009))
- かふぇらて ([@cafely_latte](https://x.com/cafely_latte))
- ウパー ([@upah_setu](https://x.com/upah_setu))
- Ray
- うぽつです ([@up2ds](https://x.com/up2ds))

## 開発に参加する

ソースからのビルド・テスト・動作の仕組みは [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)、不具合の報告・辞書の追加・Pull Request の送り方は [CONTRIBUTING.md](CONTRIBUTING.md) を見てください。
