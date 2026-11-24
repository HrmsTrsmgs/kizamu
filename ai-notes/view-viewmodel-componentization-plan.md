# ViewとViewModelの部品化 作業一覧

2026-11-24に `bb4b679` のBlazorとReactを精査した結果に基づく。01〜03・08はコミット済み。04は実装・検証済みでレビュー待ち。05〜07・09〜12は未着手。次は05の一覧の合成と終了対象の更新を整理する。精査時点では01・02をReactのReducerで再現し、Blazorはソースと既存テストで経路を確認した。

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

2026-11-24、基準は `d70d9dd`。既存のViewModel・フックのテストで開始・終了、再取得の可否と失敗通知を、画面テストでボタンと操作の接続を確認した。Blazorソリューションの.NETテスト318件、TypeScriptテスト245件、両ビルド、TypeScriptのLint、変更C#の書式検査、差分検査が成功。.NETビルドは既存のBL0016警告1件。差分はレビュー用に未コミット。

### 05　一覧の合成と終了対象の更新を通信処理から分ける

一覧取得後に、サーバー応答と取得中の操作結果を合成し、各タブの終了対象を更新する。特にBlazorではこの状態更新と通信・復旧を同じViewModelが扱う。合成に必要な一覧、操作結果、既存の割当を明確にし、通信や描画を伴わない状態更新として扱える境界を検討する。Reactの既存Reducerを活かし、遅延応答、終了対象の解除、既存の順序を同じシナリオで確かめる。 [Blazorの一覧取得と合成](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs#L82) · [Reactの状態更新](../src/frontends/react/src/presentation/timeRecorderState.ts#L39)

### 06　プロジェクトの登録とタブ選択を用途別に渡せるようにする

既存の `ProjectTabsViewModel` とReducerはプロジェクト一覧・選択と、ダイアログの開閉・入力を扱う。タブ部品には一覧と選択操作、登録部品には入力・登録・キャンセルに必要な情報を渡す。登録後に一覧と選択が更新される仕様、キャンセル時の下書き、ログアウト時の初期化は、既存のPresentationと画面の接続で確かめる。 [BlazorのプロジェクトViewModel](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/Projects/ProjectTabsViewModel.cs#L11) · [ReactのプロジェクトReducer](../src/frontends/react/src/presentation/projectTabsState.ts#L25)

## Viewの部品化

### 07　Blazorの記録画面へ渡すViewModelとその初期化を揃える

`App` から認証付きViewModelを渡す場合も、`TimeRecorder` は別のViewModelのDI登録を要求する。表示中に渡すViewModelを差し替えても初回一覧取得が走らない。画面を組み立てる箇所で使用するViewModelを決め、初回表示と差し替えで対象の一覧を一度だけ取得すること、同じViewModelでのタブ切替では再取得しないことを確認する。 [Appの生成処理](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/App.razor#L80) · [記録画面の初期化](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecorder.razor#L54)

### 08　プロジェクトタブを認証画面から独立させる

BlazorとReactで `ProjectTabs` を切り出した。部品は一覧と選択を受け取り、追加ボタンとタブの操作を親へ通知する。見出しは部品ごとのIDで参照する。認証、記録サービス、登録ダイアログの開閉は親が扱う。 [Blazorのタブ部品](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/Projects/ProjectTabs.razor) · [Reactのタブ部品](../src/frontends/react/src/projects/ProjectTabs.tsx)

2026-11-24、基準は `4a0c146`。既存の画面テストで追加とタブ選択、記録欄への反映を確認した。.NETテスト318件、TypeScriptテスト244件、両ビルド、TypeScriptのLint、書式検査と差分検査が成功。.NETビルドは既存のBL0016警告1件。`72eb893` にコミット済み。

### 09　プロジェクト登録ダイアログをタブの描画から独立させる

登録とキャンセルの接続、入力とラベル、ダイアログの見出しを部品単位で確認できるようにする。同じページに複数描画しても固定IDが衝突せず、対応する入力欄と見出しを参照できることを確認する。 [Blazorの登録欄](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/App.razor#L43) · [Reactの登録欄](../src/frontends/react/src/timeRecords/AuthenticatedTimeRecords.tsx#L52)

### 10　記録操作欄と記録一覧をそれぞれ配置できるようにする

Reactでは操作欄と一覧を分けているが、入力型が記録画面全体のReducerに依存する。Blazorでは同じ `TimeRecorder.razor` が両方を描画する。操作欄は表示する記録・状態と呼び出す操作、一覧は `TimeRecord` の列を受け取る。両部品を独立して描画できること、同じページに複数置いても見出しの参照が衝突しないことを確認する。時刻表示には既存の `TimeRecordTimes` を使う。 [Blazorの記録画面](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecorder.razor#L4) · [Reactの操作欄と一覧](../src/frontends/react/src/timeRecords/TimeRecorder.tsx#L49)

## 表示処理の共用を検討する項目

### 11　同じ進行中の記録を表示する部品の時刻更新

同じ記録を操作欄と一覧に表示すると各部品がタイマーを持つ。性能への影響は未測定。時刻更新を共用しても部品を独立して扱えるか確認する。変更する場合は分の境界で両方が更新され、終了後に時刻表示が固定され、表示を外すと不要なタイマーが解除されることを確認する。 [Blazorのタイマー](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecordTimes.razor#L39) · [Reactのタイマー](../src/frontends/react/src/presentation/useTimeRecordTimes.ts#L15)

### 12　記録欄・タブ・登録ダイアログのCSS

両実装の同じ表示に、同じセレクターと宣言が多い。既存の[全コードレビューの項目29](code-review-actions.md#L271)で既存CSS内の重複を整理した後、共通のCSSを双方から使えるか確認する。共用する場合は通常幅・狭幅とフォーカス表示、両方のビルドと静的ファイル配信を確かめる。Blazorの単独ビルドがNodeやnpmなしで完了することも確認する。 [BlazorのCSS](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/wwwroot/app.css#L39) · [ReactのCSS](../src/frontends/react/src/index.css#L57)
