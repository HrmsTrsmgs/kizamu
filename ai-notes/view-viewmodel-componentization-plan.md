# ViewとViewModelの部品化 作業一覧

2026-11-24に `bb4b679` のBlazorとReactを精査した結果に基づく。01〜10はコミット済み。11は検討済み。次は12のCSSの共用を検討する。精査時点では01・02をReactのReducerで再現し、Blazorはソースと既存テストで経路を確認した。

既存の[全コードレビューの項目27](code-review-actions.md#L253)では、Reactの認証済み画面を `App` から抽出し、Blazorの認証後のViewModel生成の重複を減らした。両実装の `TimeRecordTimes` も操作欄と一覧で共用済み。ここでは、各部品が必要とする入力、引き受ける処理、単独で確認できる振る舞いを作業単位にする。

## 着手順と検証

01・02は独立した不具合修正として、両実装のViewModel・Reducerで同じシナリオの失敗を先に確認する。03〜10は部品ごとに進め、状態や操作の規則はViewModel・Reducer・フックで検証し、画面テストでは描画、入力、ラベル、イベント接続を確認する。07のViewModel差し替え時の一覧取得も、先に失敗を再現する。11・12は現在の利用箇所で共用する効果と実装への影響を確認してから扱う。

部品名やメソッド名から責務と使い方が分かり、呼び出し側が数秒で処理を追えることを重視する。BlazorではViewModel、ReactではReducer・フックで同じ責務を確認する。初回一覧の応答にない記録の追加順は[開始順に決定](../docs/decisions.md#d015一覧に補う記録は開始した順に追加する)した。タブ切替時に操作欄にどの記録を出すかは、既存の[仕様の決定が必要な差](code-review-actions.md#仕様の決定が必要な差)で扱う。 [画面部品と状態の共用](../docs/ai-instructions/kizamu.md#画面部品と状態の共用) · [既存の責務分離方針](../docs/decisions.md#d008viewmodelと通信サービスを分け内側の仕様を直接テストする)

## 先に直す状態の不整合

### 01　再取得後に操作欄と一覧で同じ記録の終了状態を表示する

Aの終了がサーバーで成功し、応答の受信だけが失敗すると、再取得後も操作欄に古いAが残る。表示対象の選択とその記録の最新値の取得を分ける。再取得後に操作欄と一覧が同じAの終了時刻を使い、終了対象が解除されることを確認する。初回の一覧取得だけでは操作欄に記録を出さない既存仕様も維持する。 [Blazorの再取得](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs#L95) · [Reactの再取得](../src/frontends/react/src/presentation/timeRecorderState.ts#L39)

2026-11-24、基準は `839f975`。両実装で、再取得後も操作欄の終了時刻だけが空のままになることを回帰テストで確認した。再取得した一覧から表示中の記録と同じIDの最新値を反映するよう変更し、両テストが成功した。.NET全317件、TypeScript全243件、両ビルド、TypeScriptのLint、変更したC#の書式検査、差分検査が成功。全体の.NETビルドは既存のBL0016警告1件。コードとテストは `8836fc3` にコミット済み。 [Blazorの回帰テスト](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L495) · [Reactの回帰テスト](../tests/frontends/react/presentation/useTimeRecorder.test.ts#L574)

### 02　再取得した一覧にない記録を全タブの終了対象から外す

AとBで作業を開始し、Aを選択中の再取得で一覧が空になった後にBへ戻ると、Bの古い終了対象が復活する。取得中に確定した操作結果を合成した一覧と全タブの終了対象を照合する。Bで開始でき、古いIDでは終了要求を送れないことと、一覧に残る未終了記録は引き続き終了できることを確認する。 [Blazorの終了対象更新](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs#L126) · [Reactの終了対象更新](../src/frontends/react/src/presentation/timeRecorderState.ts#L46)

2026-11-24、基準は `8836fc3`。両実装で一覧から消えた記録がBの終了対象に残ることをテストで再現した。合成後の一覧で未終了と確認できない保存済みIDを解除し、BlazorではBで終了要求を送らず新規開始できること、ReactではReducerを直接実行して一覧から消えた未指定の終了対象が空になることを確認した。Blazorソリューションの.NETテスト318件、TypeScript全244件、ビルド、Lint、変更C#の書式検査、差分検査が成功。全体.NETビルドには既存BL0016警告が1件。コードとテストは `c87ca9e` にコミット済み。 [Blazorのテスト](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L979) · [Reactのテスト](../tests/frontends/react/presentation/timeRecorderState.test.ts#L102)

## ViewModel・Reducer・フックの部品化

### 03　プロジェクトの選択をタブと記録操作で共用する

Reactのタブと記録欄は、記録側のReducerが持つ選択を参照する。開始・終了は要求時のプロジェクトを保持する。画面に記録サービスを渡さなくなったときは一覧と操作状態を初期化し、ログアウト操作では選択も解除する。ログアウト前の遅延応答は再ログイン後の一覧に反映しない。 [タブと記録欄への接続](../src/frontends/react/src/timeRecords/AuthenticatedTimeRecords.tsx#L27) · [記録サービスの寿命](../src/frontends/react/src/presentation/useTimeRecorder.ts#L36)

2026-11-24、基準は `72eb893`。追加テストで、ログアウト前の開始応答が再ログイン後に届いても作業記録一覧に追加されないことを確認した。既存テストでタブ切替中の遅延応答と再ログイン時の初期化も確認した。ワーカーを2つに制限してTypeScript全245件が成功し、Lint・ビルド・差分検査も成功。`d70d9dd` にコミット済み。

### 04　記録操作の実行と失敗時の通知を画面から切り出す

BlazorのViewModelとReactのフックが、記録ボタンで実行する開始・終了を選ぶ。操作欄は記録状態、操作可否、通知を描画し、記録操作と手動再取得を呼ぶ。Blazorでは初回取得と手動再取得を別の入口とし、手動操作の失敗はViewModelが通知に変える。 [Blazorの操作](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs#L63) · [Reactの操作フック](../src/frontends/react/src/presentation/useTimeRecorder.ts#L126)

2026-11-24、基準は `d70d9dd`。既存のViewModel・フックのテストで開始・終了、再取得の可否と失敗通知を、画面テストでボタンと操作の接続を確認した。Blazorソリューションの.NETテスト318件、TypeScriptテスト245件、両ビルド、TypeScriptのLint、変更C#の書式検査、差分検査が成功。.NETビルドは既存のBL0016警告1件。`2368e39` にコミット済み。

### 05　一覧の合成と終了対象の更新を通信処理から分ける

Blazorでは `TimeRecorderState` が、一覧応答と取得中の操作結果の合成、表示中の記録、各タブの終了対象を更新する。ViewModelは通信、応答を採用する条件、失敗時の再取得を扱う。Reactは既存のReducerで状態を更新する。 [Blazorの状態更新](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderState.cs) · [Reactの状態更新](../src/frontends/react/src/presentation/timeRecorderState.ts#L39)

2026-11-24、基準は `2368e39`。Blazorの合成順と取得中に開始した記録の優先を確認する2件を状態の直接テストに移した。Reactと同じく、別タブを表示中に一覧から消えた未指定の記録が終了対象から外れることも直接確認した。ViewModelには遅延応答の接続テストを残した。Blazorソリューションの.NETテスト319件と対象Reactテスト60件、.NETビルド、書式・差分検査が成功。.NETの既存BL0016警告は1件。`b1fb9b7` にコミット済み。

### 06　プロジェクトの登録とタブ選択を用途別に渡せるようにする

Blazorは `ProjectRegistrationViewModel` が登録欄の下書きと操作を公開し、登録結果をタブの一覧に渡す。Reactは `useProjectTabs` が既存Reducerにつなぎ、一覧と登録欄の値・操作を分けて画面に返す。登録時はタブを追加し、現在の選択は維持する。キャンセル時は下書きを保持し、ログアウト時は初期化する。 [Blazorの登録ViewModel](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/Projects/ProjectRegistrationViewModel.cs) · [Reactのフック](../src/frontends/react/src/presentation/useProjectTabs.ts)

2026-11-24、基準は `b1fb9b7`。登録・キャンセルの状態はPresentationで、ログアウト時の下書き初期化は両実装の画面テストで確認した。.NET 321件、TypeScript 247件、両ビルド、Lint、書式・差分検査が成功。.NETビルドは既存のBL0016警告1件。`0a1502a` にコミット済み。

## Viewの部品化

### 07　Blazorの記録画面へ渡すViewModelとその初期化を揃える

`App` が初期認証済み表示用のViewModelかログイン時に作ったViewModelを選び、`TimeRecorder` に渡す。記録画面は初回表示とViewModelの差し替えで一覧を一度取得する。同じViewModelでタブを切り替えたときは一覧を再取得しない。 [Appでの受け渡し](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/App.razor#L33) · [記録画面の初期化](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecorder.razor#L63)

2026-11-24、基準は `0a1502a`。Redでは差し替え先への一覧取得が期待1回に対し0回となり、同じViewModelでのタブ切替は合格した。Greenでは初回またはViewModelの参照が変わったときに一覧取得を開始するようにした。.NET 323件、Blazorのビルド、書式・差分検査が成功。既存のBL0016警告は1件。`8cb56f4` にコミット済み。

2026-11-24、基準は `8cb56f4`。明示したViewModelだけで表示するテストは、Redで `ITimeRecorderViewModel` のDI登録がないため失敗し、Greenで合格した。`TimeRecorder` のViewModel引数を必須とし、`App` の初期認証済み表示もViewModelを直接受け取る形に変更した。.NET 324件、Blazorのビルド、書式・差分検査が成功。既存のBL0016警告は1件。ViewModel受け渡しの修正は `0584e1a` にコミット済み。

### 08　プロジェクトタブを認証画面から独立させる

BlazorとReactで `ProjectTabs` を切り出した。部品は一覧と選択を受け取り、追加ボタンとタブの操作を親へ通知する。見出しは部品ごとのIDで参照する。認証、記録サービス、登録ダイアログの開閉は親が扱う。 [Blazorのタブ部品](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/Projects/ProjectTabs.razor) · [Reactのタブ部品](../src/frontends/react/src/projects/ProjectTabs.tsx)

2026-11-24、基準は `4a0c146`。既存の画面テストで追加とタブ選択、記録欄への反映を確認した。.NETテスト318件、TypeScriptテスト244件、両ビルド、TypeScriptのLint、書式検査と差分検査が成功。.NETビルドは既存のBL0016警告1件。`72eb893` にコミット済み。

### 09　プロジェクト登録ダイアログをタブの描画から独立させる

Blazor・Reactとも `ProjectRegistrationDialog` が下書きと変更・登録・キャンセルの操作を受け取る。親がダイアログの表示を決める。見出しと名前入力欄のIDは部品ごとに割り当てる。 [Blazorの部品](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/Projects/ProjectRegistrationDialog.razor) · [Reactの部品](../src/frontends/react/src/projects/ProjectRegistrationDialog.tsx)

2026-11-24、基準は `0584e1a`。両実装の部品テストで、同じページに二つ置いたときの見出しとラベルの参照を確認した。既存の画面テストで入力・登録・キャンセルの接続を確認した。.NET 326件とTypeScript 249件のテスト、両ビルド、Lint、書式・差分検査が成功。既存のBL0016警告は1件。`7b234a6` にコミット済み。

### 10　記録操作欄と記録一覧をそれぞれ配置できるようにする

BlazorとReactの `TimeRecorderPanel` はプロジェクト名、表示する記録、操作可否・通知とボタン操作を受け取る。`TimeRecordList` は `TimeRecord` の列を渡された順序で表示する。時刻の表示には既存の `TimeRecordTimes` を使う。 [Blazorの呼び出し側](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecorder.razor) · [Reactの呼び出し側](../src/frontends/react/src/timeRecords/TimeRecorder.tsx)

2026-11-24、基準は `7b234a6`。操作欄と一覧をそれぞれ単独で描画し、同じページに二つ置いたときの見出し参照を確認した。操作ボタン・通知・件数・順序のテストを対応する部品へ移し、画面と記録操作の接続テストを残した。.NET 328件、TypeScript 251件、両ビルド、Lint、書式・差分検査が成功。既存のBL0016警告は1件。`4bef875` にコミット済み。

## 表示処理の共用を検討する項目

### 11　同じ進行中の記録を表示する部品の時刻更新

2026-11-24、基準は `4bef875`。同じ進行中の記録を操作欄と一覧に表示し、仮想時計を1秒ずつ60秒進めた。両実装ともタイマーは2本で、Blazorはタイマー通知120回・時刻欄の再描画120回、Reactは表示計算での現在時刻取得120回だった。1分に達すると両方の表示が進み、操作欄を外すと更新は1秒に1回、残った記録を終了すると0回になった。終了後の表示も固定された。実ブラウザーのCPU負荷は未測定。 [Blazorのタイマー](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecordTimes.razor#L39) · [Reactのタイマー](../src/frontends/react/src/presentation/useTimeRecordTimes.ts#L15)

現在の画面で同じ記録を重ねて表示する場所は操作欄の1か所。タイマーを一つにまとめても各表示への更新通知は必要で、購読の管理が増えるため、各部品が持つタイマーを維持する。計測用ファイルは除去済み。既存の時刻表示とタイマー関連のBlazor 13件、React 17件および差分検査が成功。検討結果を記録済み。

### 12　記録欄・タブ・登録ダイアログのCSS

両実装の同じ表示に、同じセレクターと宣言が多い。既存の[全コードレビューの項目29](code-review-actions.md#L271)で既存CSS内の重複を整理した後、共通のCSSを双方から使えるか確認する。共用する場合は通常幅・狭幅とフォーカス表示、両方のビルドと静的ファイル配信を確かめる。Blazorの単独ビルドがNodeやnpmなしで完了することも確認する。 [BlazorのCSS](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/wwwroot/app.css#L39) · [ReactのCSS](../src/frontends/react/src/index.css#L57)
