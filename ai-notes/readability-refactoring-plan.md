# Kizamu 可読性リファクタリング実行計画

**今回の最優先目標は可読性の向上である。** 既存の振る舞いを保ち、製品コードではメソッドの役割と処理の流れを、テストでは前提・操作・期待結果を一読で追えるようにする。案を選ぶときは、変更前後でどの条件や操作の意味が読み取りやすくなるかを具体的に説明する。メソッド・ファイル・差分の行数やテスト件数は、案の採否にも改善の評価にも使わない。意味のある名前を付けたり、操作を実行順に明示したりする方が伝わるなら、行数が増えてもその形を選ぶ。

対象はBlazor・React、ASP.NET Core・Node、.NET・TypeScriptのDomain、これらの全テストと補助コード、自作の起動・検証スクリプト。各作業単位で個人共通、Kizamu固有、対象技術・工程の指示を確認し、適用される規約をすべて守る。変更範囲、テストの書き方、検証・レビュー・Gitの手順もこれに従う。[Kizamuの作業指示](../AGENTS.md)

## 画面ロジックの実装とテスト

* **ViewModel・Reducerに書ける画面ロジックはそこに実装する。** Blazorでは画面状態、選択対象、操作可否の判断をViewModelに置き、必要なViewModelがなければ追加する。Reactでは状態とアクションから次の状態を決める処理をReducerに置く。非同期の操作はフックで調整し、通信はサービスに委ねる。画面コンポーネントは状態を表示し、利用者の入力やクリックを対応する操作に渡す。
* **ViewModel・Reducerで確かめられる振る舞いは、原則として画面を描画しない単体テストで保証する。** BlazorではViewModelの公開操作を直接呼び、ReactではReducerに状態とアクションを渡して結果を確認する。前提も内側の公開操作や初期状態で用意する。画面テストに残すのは、表示・入力など画面でしか観測できない振る舞いと、操作が内側へ届く代表的な接続である。

例えば再開始時には、終了済みの状態から再開始すると前回の終了時刻が消えることをViewModel・Reducerで直接検証する。画面側では、再開始後の状態を与えたときに終了時刻の欄が表示されないことを確認する。[画面とテストの責務](../docs/ai-instructions/kizamu.md#domain画面テストの責務)

## 実施手順

1. **対象ファイルを一覧にする。** 先行する修正が完了し、その結果を取り込んだ作業版で基準コミットと未コミット差分を確認する。`src/` の製品コード、`tests/` の全テスト（Skip中を含む）と補助コード、`scripts/` の自作スクリプトを列挙する。
2. **各ファイルを先頭から末尾まで一行ずつ精査する。** 実装と対応するテストを照らし、各行の役割を前後の文脈から確かめる。検索は呼び出し元・依存先の確認や、読んで見つけた重複の照合に使う。ファイルごとに読了状況と修正・維持・保留の理由を記録する。中断時は次に読むファイルと行を残す。途中で内容が変わったファイルと新しく加わったファイルは、更新後の版で通読する。
3. **一つの振る舞いごとに変更する。** 精査で見つけた箇所について、読み取りにくい理由と、その振る舞いを保証する既存テストを特定する。対象テストを変更前に確認し、保証が足りない箇所は先に補う。対応する実装をBlazorとReact、ASP.NET CoreとNodeで照合し、必要な箇所を小さな差分で直す。
4. **変更後に保証を確かめる。** 対象テストを再実行し、元の期待結果をどのテストが保証するかを確認する。.NETは関係するソリューションのビルド・テストと変更したC#のformat検査、TypeScriptは `npm run lint`・`npm test`・`npm run build` を行う。今回は開発者の了承によりコミット前の人間レビューを省き、対象ファイルと検証結果を照合して、一つのリファクタリングにつき一つのコミットを作る。[ビルドと検証](../README.md#ビルドと検証)

## 製品コードとテストに共通する判断基準

* **命名と責務。** メソッド・関数の役割を既存の業務用語で一言で表し、本文を数秒で読んで流れをつかめる粒度にする。複数の役割が混ざる場合は、呼び出しを順に読めば手順が分かる単位に分ける。
* **重複の削除。** 同じ仕様変更で一緒に直す処理は、同じ役割の既存コードを優先して使い、規則を一か所で読めるようにする。たとえば記録の開始と終了では、失敗後の一覧再取得を各実装内で共通化し、いつ再取得するかは各操作から追える。見た目が似ていても変更理由が異なる処理や、共通化すると用途を切り替える引数・分岐で意図が隠れる処理は、個別に保つ。
* **条件と処理の流れ。** 入れ子の分岐や複数の判断を含む式は、業務上の意味を表す条件名や中間値を使い、正常時と例外時の動きを追える形にする。式の抽出やインライン化では、評価回数とタイミング、副作用の順序、例外の伝わり方を保つ。

## これまでのリファクタリングから引き継ぐ注意点

* **準備と検証対象を区別する。** 記録の一覧テストでは「開始済みの記録を用意する」と前提を表し、二件を開始する順序と、二件目の前に時計を進める操作は本文に残した。開始API自体のテストでは、開始要求とその期待結果を本文に置く。中継先の構築やサーバーの起動待ち・後始末のように、一度しか使わない準備でも主題を読み取りやすくするなら切り出す。応答を待つ順序、失敗時の後始末、リソースの解放範囲を保つ。[テスト準備の精査記録](test-preparation-review.md)
* **共通準備と個別準備を分ける。** テストごとに新しく用意する共通の対象・依存は、C#ではテストクラスのコンストラクター、Vitestでは `beforeEach` に置く。残る個別準備には前提を表す名前を付け、複数の結果が必要なときはC#の名前付きタプルやTypeScriptのオブジェクトで返し、呼び出し側で分解する。直接読める準備や結果を左右する条件は本文に置く。
* **一つのテストで一つの振る舞いを読む。** Reactで再開始の確認を開始・終了の接続確認から分けたように、独立して説明できる振る舞いは分ける。一つの結果を示す複数のAssertは同じテストに置き、入力・操作・期待結果のつながりを保つ。
* **検証する仕様に応じて公開境界を選ぶ。** 業務規則はDomain、HTTPの形式やDTO変換は通信境界で直接確認する。モックの準備が主題を隠す場合は、どの公開操作を使えば前提と振る舞いを明確に読めるかを見直す。
* **期待値が保証する意味を保つ。** 値と順序に加えてDomainクラスも保証するときは、期待するインスタンスを含む一覧やオブジェクトを `should.toStrictEqual` で直接比較する。通常のオブジェクトとの違いに加え、`undefined` のキーや疎配列の扱いも照合する。日時やHTTP JSONなど値を保証するテストは、その意図に合う比較を選ぶ。[テストの記述方針](../docs/testing-style.md)

## 完了条件

対象一覧と完了時の作業版を照合し、すべての対象ファイルに最終行までの精査結果があることを確認する。各変更で従来の振る舞い、テスト間の独立性、非同期処理の順序、リソースの破棄が保たれ、関連する検証が通っていることを確かめる。画面から移した状態仕様は、元の条件と期待結果を内側の直接テストで保証できたことを確認してから、重複する画面テストを整理する。保留事項と既存のSkipは理由を記録する。

## 第2回の精査（2026-11-12）

基準はPR #21の統合後の `main`（`23b4932`）、最終コードは `b1dee7c`。基準版の176ファイルを一行ずつ再読した。既存16ファイルを変更し、4ファイルを追加した。変更・追加した20ファイルは最終版を再読し、残る160ファイルは基準と同じ内容であることをGitで照合した。

| 範囲 | 開始時 | 最終版 | 精査した責務と維持の理由 |
| --- | ---: | ---: | --- |
| Blazor | 47 | 49 | 画面に残すボタン・通知・時刻属性の接続と、ViewModelで検証する状態を照合した。HTTPの変換、配信、プロキシはそれぞれの入口で確認する。 |
| React | 48 | 50 | 入力・表示の接続、通信の待機と失敗、Reducerの状態遷移を区別した。実際のサービス応答や選択の順序を必要とするケースはフックで検証する。 |
| ASP.NET Core | 32 | 32 | HTTP、開始・終了操作、保存状態が返す一覧の参照と列挙の契約を照合した。実際のルートと応答はAPIテストに残した。 |
| Node | 21 | 21 | HTTPの応答、記録を置き換える順序、サーバーの起動と解放を照合した。個々の契約を観測できるテストを維持した。 |
| .NET Domain | 7 | 7 | 生成・復元・終了はDomainの公開操作で追える。終了が同じ記録を更新することと、依存先の検査を維持した。 |
| TypeScript Domain | 16 | 16 | 不変な記録の生成・復元・再終了、日時点からの期間計算、依存方向を照合した。復元時の各値は個別に検証する。 |
| 共通テスト補助 | 2 | 2 | xUnitのキャンセル取得とVitestのshould登録は各テスト基盤に固有の準備としてまとまっている。 |
| 起動スクリプト | 3 | 3 | 待機を共用する3本を通読した。BlazorのプロセスとReactの子プロセスでは停止方法が異なり、各入口で起動失敗時の解放順を追える。 |

### リファクタリングとコミット

| コミット | 読みやすくした箇所 |
| --- | --- |
| `fabb8f9` | Reactの競合テストで、最初の要求・重複操作・応答・再試行の順序を本文に出し、共通モックの二重受け取りを解消した。 |
| `f5e8a7f` | プロジェクトの入力名と登録を同じReducerで扱う。入力直後のクリックで最後に入力した名前がタブに現れることを変更前後で確認した。 |
| `d2b9f4a` | Blazorの再描画タイマーを分離した。再設定、記録終了、破棄は時計を進めて直接検証し、表示の分境界と終了後の再描画停止はRazorとの接続でも確認する。 |
| `66af17d` | Nodeの再終了テストでは、対象記録ともう一件を保存契約の `add` で用意する。採番や内部配列の管理を本文から取り除いた。 |
| `9ee862e` | 開始・終了・再終了で返す `TimeRecord` を期待するインスタンスと厳密比較する。元の記録の値と参照、復元コンストラクターに渡した各値は個別に照合する。 |
| `a7991a3` | ASP.NET Coreの一覧APIテストで、終了済み記録を作る通信と成功の確認を準備にまとめた。開始・終了時刻、一覧GET、期待するJSON配列が本文で読める。 |
| `e8613af` | 終了操作テストで一件を開始して保存する手順を共通化した。どの記録を再終了し、どの記録が変化しないかを本文に残した。 |
| `3a0ed83` | Blazorのプロジェクト別の終了テストで、コンストラクターが用意したViewModelとサービスを使う。各ケースで変わる終了応答だけを指定する。 |
| `d85b85e` | Blazorの通信テストでJSON応答の生成をテスト用HTTPハンドラーに集約した。要求ごとに新しい応答を返し、各テストにはステータスと本文の期待値を残す。 |
| `2fc8aa7` | Reactで、一覧の取得開始から終了までに受け取った更新を要求ごとに保持する処理を分離した。重なる二つの取得と片方の終了後を直接検証し、失敗後に別の応答が届く順序はフックでも確認する。 |
| `b1dee7c` | 再開始時に前回の記録を残して終了時刻を消す仕様をReducerで直接確認する。別プロジェクトを表示中に開始応答が届くときの状態と、実際の通信・選択順の接続をそれぞれ検証する。 |

### 第2回で変更・追加したファイル

* **Blazor**　[TimeRecordTimes.razor](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecordTimes.razor)、[TimeRecordRefreshTimer.cs](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecordRefreshTimer.cs)、[TimeRecordRefreshTimerTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecordRefreshTimerTests.cs)、[HealthApiTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure.Tests/Health/HealthApiTests.cs)、[TimeRecordApiTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure.Tests/TimeRecords/TimeRecordApiTests.cs)、[TimeRecorderViewModelTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs)、[TestHttpMessageHandler.cs](../tests/frontends/blazor/TestSupport/TestHttpMessageHandler.cs)
* **React**　[App.tsx](../src/frontends/react/src/App.tsx)、[pendingTimeRecordLoads.ts](../src/frontends/react/src/presentation/pendingTimeRecordLoads.ts)、[projectTabsState.ts](../src/frontends/react/src/presentation/projectTabsState.ts)、[useTimeRecorder.ts](../src/frontends/react/src/presentation/useTimeRecorder.ts)、[App.test.tsx](../tests/frontends/react/App.test.tsx)、[pendingTimeRecordLoads.test.ts](../tests/frontends/react/presentation/pendingTimeRecordLoads.test.ts)、[projectTabsState.test.ts](../tests/frontends/react/presentation/projectTabsState.test.ts)、[timeRecorderState.test.ts](../tests/frontends/react/presentation/timeRecorderState.test.ts)、[useTimeRecorder.test.ts](../tests/frontends/react/presentation/useTimeRecorder.test.ts)
* **ASP.NET Core**　[TimeRecordEndpointTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/TimeRecordEndpointTests.cs)、[TimeRecordEndOperationTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordEndOperationTests.cs)
* **Node**　[timeRecords.test.ts](../tests/backends/node/timeRecords.test.ts)
* **TypeScript Domain**　[timeRecord.test.ts](../tests/domains/typescript/timeRecord.test.ts)

### 最終検証

* .NETは `Kizamu.Blazor.slnx` の16プロジェクトの非増分ビルド、全160テスト、ソリューション全体のformat検査が成功した。ビルドの警告・エラーと16プロジェクトのSARIF診断はいずれも0件。
* TypeScriptはルートの `npm run lint`、`npm test`、`npm run build` が成功した。Domain 13件、React 115件、Node 23件の計151件。両技術とも失敗・スキップは0件。
* ChromeでReact＋Node、Blazor＋ASP.NET Coreを起動し、接続表示、追加のキャンセル、入力したプロジェクトの登録・選択、記録開始・終了を確認した。両画面とも操作欄・一覧の開始時刻と終了時刻がAPIの応答に一致した。
* 一覧取得が重なる場合は従来の応答順を維持した。古い初回応答で未確認状態が解除される既知の制約は[README](../README.md)に記載されている。

## 精査記録

2026-11-12に完了。開始版 `312279e` の `src/`・`tests/`・`scripts/` にあった166ファイルを先頭から最終行まで精査した。最終コード版は `859955a`。追加12件、削除2件をGitの一覧と突き合わせ、最終版176ファイルの判断を以下に記す。変更・新規ファイルは最終版を再読し、維持したファイルは開始時と内容が一致することを照合した。

削除した `src/frontends/react/src/presentation/useProjectTabs.ts` と `tests/frontends/react/presentation/useProjectTabs.test.ts` は、プロジェクトの登録・選択・キャンセルを扱う [Reducer](../src/frontends/react/src/presentation/projectTabsState.ts) と[直接テスト](../tests/frontends/react/presentation/projectTabsState.test.ts) に置き換えた。25件のリファクタリングをそれぞれ独立してコミットした。

### 検証

* .NETは `Kizamu.Blazor.slnx` の16プロジェクトを非増分ビルドし、警告・エラー0件、全157テスト成功、スキップ0件。全体の `dotnet format --verify-no-changes --severity info` とビルド時のアナライザー診断も0件だった。
* TypeScriptはルートの `npm run lint`、`npm test`、`npm run build` が成功した。テストはDomain 13件、React 111件、Node 23件の計147件で、失敗・スキップは0件だった。
* Reactの遅延一覧と操作結果の合成は、旧版・新版に同じ96,844通りの入力を渡し、終了対象、一覧の順序、各記録の参照の一致を確認した。フックテストの先行する未コミット4差分は元のまま保全し、作業版とコミット版のTypeScript構文木も一致した。
* 起動待機は成功・再試行・時間切れ・子プロセス終了を含む8ケースで旧2実装と新関数の結果を照合した。両入口の成功・失敗時の後始末も確認した。CSSはChromeの4画面幅で変更前後の計算済みスタイルを比較し、両クライアントで一致した。
* 実際のブラウザーでもReact＋Node、Blazor＋ASP.NET Coreをそれぞれ起動した。Chromeで正常性表示、追加のキャンセル、プロジェクト登録と選択、記録開始・終了、完了した一覧を操作し、画面の開始・終了時刻とAPIの記録が一致した。

### 起動確認（`scripts/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| [Check-Blazor.ps1](../scripts/Check-Blazor.ps1)、[Check-React.ps1](../scripts/Check-React.ps1)、[Wait-HealthEndpoint.ps1](../scripts/Wait-HealthEndpoint.ps1) | 修正。両クライアントが同じ待機関数を使う。プロセス終了・再試行・タイムアウトを維持し、各入口で固有のログと終了処理を読める。 |

### .NET Domain（`src/domains/dotnet/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| `Kizamu.Domain/Kizamu.Domain.csproj` | 維持。各プロジェクトに必要な対象フレームワーク、参照方向、テスト依存を宣言している。 |
| `Kizamu.Domain/Project.cs`、`Kizamu.Domain/TimeRecord.cs` | 維持。名前、開始・復元・終了、期間の規則をDomainで直接表現。言語ごとの同一性と更新方法を維持。 |

### TypeScript Domain（`src/domains/typescript/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| `eslint.config.js` | 維持。各層の対象・禁止依存・環境を明示。設定を共有する範囲は既存のまま。 |
| `index.ts` | 維持。Domainの公開型と期間計算を列挙する入口。 |
| `kizamu-domain-typescript.esproj`、`package.json`、`tsconfig.json` | 維持。実行入口、型検査、依存先またはテスト起動を宣言。構成の差はツールや層に対応。 |
| `project.ts`、`timeRecord.ts`、`timeRecordDuration.ts` | 維持。名前、開始・復元・終了、期間の規則をDomainで直接表現。言語ごとの同一性と更新方法を維持。 |

### ASP.NET Core（`src/backends/aspnetcore/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| `Kizamu.Backend.AspNetCore.Api/Endpoints/Health/HealthEndpoint.cs`、`Kizamu.Backend.AspNetCore.Api/Endpoints/TimeRecords/EndTimeRecordEndpoint.cs`、`Kizamu.Backend.AspNetCore.Api/Endpoints/TimeRecords/GetTimeRecordsEndpoint.cs`、`Kizamu.Backend.AspNetCore.Api/Endpoints/TimeRecords/StartTimeRecordEndpoint.cs` | 維持。HTTPの経路・結果コード・アプリケーション操作が一つの登録で読める。 |
| `Kizamu.Backend.AspNetCore.Api/Endpoints/TimeRecords/TimeRecordResponse.cs` | 維持。開始・終了・一覧の各結果からJSON応答への対応を型ごとに明示。 |
| `Kizamu.Backend.AspNetCore.Api/Kizamu.Backend.AspNetCore.Api.csproj`、`Kizamu.Backend.AspNetCore.Application/Kizamu.Backend.AspNetCore.Application.csproj`、`Kizamu.Backend.AspNetCore.Infrastructure/Kizamu.Backend.AspNetCore.Infrastructure.csproj` | 維持。各プロジェクトに必要な対象フレームワーク、参照方向、テスト依存を宣言している。 |
| `Kizamu.Backend.AspNetCore.Api/Kizamu.Backend.AspNetCore.Api.http` | 維持。正常性確認の要求を単独で試せる。 |
| [Kizamu.Backend.AspNetCore.Api/Program.cs](../src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api/Program.cs) | 修正。サービス登録と同一オリジン判定を分け、保護対象を経路の登録箇所で示す。SDKが生成する公開Programの型・コンストラクター・実行入口を確認した。 |
| `Kizamu.Backend.AspNetCore.Api/Properties/launchSettings.json`、`Kizamu.Backend.AspNetCore.Api/appsettings.Development.json`、`Kizamu.Backend.AspNetCore.Api/appsettings.json` | 維持。ローカルの起動先・ログ・経路を該当するホストが所有している。 |
| `Kizamu.Backend.AspNetCore.Application/TimeRecords/ITimeRecordState.cs`、`Kizamu.Backend.AspNetCore.Infrastructure/TimeRecords/InMemoryTimeRecordState.cs` | 維持。参照一覧とコピーの意味・ロック範囲を区別。スナップショットの取得時点を維持。 |
| `Kizamu.Backend.AspNetCore.Application/TimeRecords/TimeRecordEndOperation.cs`、`Kizamu.Backend.AspNetCore.Application/TimeRecords/TimeRecordReader.cs`、`Kizamu.Backend.AspNetCore.Application/TimeRecords/TimeRecordStartOperation.cs` | 維持。開始・終了・遅延列挙が別の操作。未知IDの扱いと列挙開始時点を保持。 |

### Node（`src/backends/node/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| `.vscode/launch.json` | 維持。ローカルの起動先・ログ・経路を該当するホストが所有している。 |
| `api/timeRecordEndpoints.ts`、`app.ts`、`server.ts` | 維持。起動、アプリの組立、保護された更新経路と公開GETのHTTP登録を照合。 |
| `application/timeRecords.ts`、`infrastructure/timeRecordSources.ts`、`infrastructure/timeRecordState.ts` | 維持。時計・ID・保存先を注入し、開始・終了・順序の更新を分離。 |
| `eslint.config.js` | 維持。各層の対象・禁止依存・環境を明示。設定を共有する範囲は既存のまま。 |
| `kizamu-backend-node.esproj`、`package.json`、`tsconfig.json` | 維持。実行入口、型検査、依存先またはテスト起動を宣言。構成の差はツールや層に対応。 |

### Blazor（`src/frontends/blazor/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| `Kizamu.Frontend.Blazor.Client/App.razor`、`Kizamu.Frontend.Blazor.Presentation/Projects/ProjectTabsViewModel.cs` | 維持。タブの選択・登録はViewModelが保持。Appの入力・選択表示と状態検証の重複、共通フィクスチャを照合。 |
| [Kizamu.Frontend.Blazor.Client/Health/HealthStatus.razor](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/Health/HealthStatus.razor)、[Kizamu.Frontend.Blazor.Client/Program.cs](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/Program.cs)、[Kizamu.Frontend.Blazor.Client/_Imports.razor](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/_Imports.razor)、[Kizamu.Frontend.Blazor.Infrastructure/Health/HealthApi.cs](../src/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure/Health/HealthApi.cs) | 修正。接続状態はViewModel、HTTPはサービス、Razorは日本語表示と初期化を担当。画面試験の準備をサービス境界に揃えた。 |
| `Kizamu.Frontend.Blazor.Client/Kizamu.Frontend.Blazor.Client.csproj`、`Kizamu.Frontend.Blazor.Infrastructure/Kizamu.Frontend.Blazor.Infrastructure.csproj`、`Kizamu.Frontend.Blazor.Presentation/Kizamu.Frontend.Blazor.Presentation.csproj`、`Kizamu.Frontend.Blazor.Server/Kizamu.Frontend.Blazor.Server.csproj` | 維持。各プロジェクトに必要な対象フレームワーク、参照方向、テスト依存を宣言している。 |
| [Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecordTimes.razor](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecordTimes.razor)、[Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecordTimesFormatter.cs](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecordTimesFormatter.cs) | 修正。日本時間への変換・経過時間の計算を直接テストし、HTMLの時刻属性とタイマーの再描画・停止を画面で確認する。 |
| [Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecorder.razor](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecorder.razor)、[Kizamu.Frontend.Blazor.Presentation/TimeRecords/ITimeRecorderViewModel.cs](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/ITimeRecorderViewModel.cs)、[Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs) | 修正。操作の可否と失敗通知をViewModelで決め、一覧取得中の更新・終了対象・失敗時の再取得を各操作から読めるよう分けた。画面は表示と入力を接続する。 |
| [Kizamu.Frontend.Blazor.Client/wwwroot/app.css](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/wwwroot/app.css) | 修正。ヘッダー余白を決める宣言を一つにした。Blazorではタブの隣接する同じセレクターもまとめ、選択状態と画面幅ごとの表示値を保った。 |
| `Kizamu.Frontend.Blazor.Client/wwwroot/index.html`、`Kizamu.Frontend.Blazor.Server/Program.cs` | 維持。起動HTML、静的ファイル、API転送、フォールバックの順序が明確。 |
| `Kizamu.Frontend.Blazor.Infrastructure/TimeRecords/TimeRecordApi.cs`、`Kizamu.Frontend.Blazor.Infrastructure/TimeRecords/TimeRecordResponse.cs` | 維持。HTTP応答の破棄とDomainへの変換を通信境界で完結。成功・失敗・JSONを直接確認。 |
| [Kizamu.Frontend.Blazor.Presentation/Health/HealthStatusViewModel.cs](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/Health/HealthStatusViewModel.cs)、[Kizamu.Frontend.Blazor.Presentation/Health/IHealthService.cs](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/Health/IHealthService.cs) | 修正。応答待ち・正常・異常・通信失敗をHTTPや画面なしで再現し、接続状態を直接検証する。 |
| `Kizamu.Frontend.Blazor.Presentation/TimeRecords/ITimeRecordService.cs` | 維持。開始・終了・再取得の操作可否と失敗表示、遅延一覧の合成、共通準備をViewModelと画面で照合。 |
| [Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderNotice.cs](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderNotice.cs) | 修正。未確認と過去の操作失敗が重なるときに出す通知を一つ選ぶ。手動再取得後に戻る通知もViewModelで直接確認する。 |
| `Kizamu.Frontend.Blazor.Server/Properties/launchSettings.json`、`Kizamu.Frontend.Blazor.Server/appsettings.json` | 維持。ローカルの起動先・ログ・経路を該当するホストが所有している。 |

### React（`src/frontends/react/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| [eslint.config.js](../src/frontends/react/eslint.config.js) | 修正。Presentationに適用する依存規則を一つにまとめ、Reactの共通ルールを項目ごとに読めるようにした。 |
| `index.html`、`kizamu-frontend-react.esproj`、`package.json`、`src/main.tsx`、`src/vite-env.d.ts`、`tsconfig.app.json`、`tsconfig.json`、`tsconfig.node.json`、`vite.config.ts` | 維持。起動HTML、依存組立、Vite・JSX・Vitest・Visual Studioの各入口と対象を確認。 |
| [src/App.tsx](../src/frontends/react/src/App.tsx)、[src/presentation/projectTabsState.ts](../src/frontends/react/src/presentation/projectTabsState.ts) | 修正。登録、キャンセル、選択をReducerとその直接テストに置き、入力・クリック・タブ表示の接続はAppで確かめる。 |
| `src/health/HealthStatus.tsx`、`src/presentation/healthService.ts`、`src/presentation/useHealthStatus.ts` | 維持。正常性の判定・非同期応答と、通知領域の日本語表示が分離されている。 |
| [src/index.css](../src/frontends/react/src/index.css) | 修正。ヘッダー余白を決める宣言を一つにした。Blazorではタブの隣接する同じセレクターもまとめ、選択状態と画面幅ごとの表示値を保った。 |
| [src/infrastructure/healthService.ts](../src/frontends/react/src/infrastructure/healthService.ts) | 修正。成功したHTTP応答の未知のJSONについて、statusがokかを直接判定する。異なる型のstatus、非成功応答の本文読取り、解析例外の伝播を照合した。 |
| [src/infrastructure/timeRecordService.ts](../src/frontends/react/src/infrastructure/timeRecordService.ts) | 修正。開始・終了のPOSTとDTO変換を共有し、通信テストは型付きfetchと標準JSON応答で経路・失敗・Domain変換を保証する。 |
| `src/presentation/timeRecordPresentation.ts`、`src/presentation/timeRecordService.ts`、`src/presentation/useTimeRecordTimes.ts`、`src/timeRecords/TimeRecordTimes.tsx` | 維持。Domainと元のISO文字列、タイマーの寿命、表示用時刻、HTML属性の保証範囲を照合。 |
| [src/presentation/timeRecorderOperationState.ts](../src/frontends/react/src/presentation/timeRecorderOperationState.ts)、[src/presentation/useTimeRecorder.ts](../src/frontends/react/src/presentation/useTimeRecorder.ts)、[src/timeRecords/TimeRecorder.tsx](../src/frontends/react/src/timeRecords/TimeRecorder.tsx) | 修正。操作の排他・再取得と、通知の優先順位をReducer・フックで表現。画面からの入力と状態別の表示を確かめる。 |
| [src/presentation/timeRecorderState.ts](../src/frontends/react/src/presentation/timeRecorderState.ts) | 修正。取得中の更新を一覧へ反映する順序と、終了対象の決定を別々に読める。重複ID、順序、参照、ISO表記を直接検証した。 |

### .NET Domainテスト（`tests/domains/dotnet/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| `Kizamu.Domain.Tests/DomainDependenciesTests.cs` | 維持。参照可能なアセンブリは各層で異なる。許可する依存先をテスト本文から読める。 |
| `Kizamu.Domain.Tests/Kizamu.Domain.Tests.csproj` | 維持。各プロジェクトに必要な対象フレームワーク、参照方向、テスト依存を宣言している。 |
| `Kizamu.Domain.Tests/ProjectTests.cs`、`Kizamu.Domain.Tests/TimeRecordTests.cs` | 維持。識別子・時刻・元の記録の不変性・期間をDomainだけで検証。 |

### TypeScript Domainテスト（`tests/domains/typescript/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| `dependencyBoundaries.test.ts` | 維持。ESLintが報告する規則と許可する参照を実際の設定に照合。 |
| `eslint.config.js` | 維持。各層の対象・禁止依存・環境を明示。設定を共有する範囲は既存のまま。 |
| `kizamu-domain-typescript-tests.esproj`、`package.json`、`tsconfig.json`、`vitest.config.ts` | 維持。実行入口、型検査、依存先またはテスト起動を宣言。構成の差はツールや層に対応。 |
| [timeRecord.test.ts](../tests/domains/typescript/timeRecord.test.ts) | 修正。コンストラクターによる復元と再終了時の元記録の不変性を独立して読める。 |
| `timeRecordDuration.test.ts` | 維持。識別子・時刻・元の記録の不変性・期間をDomainだけで検証。 |

### ASP.NET Coreテスト（`tests/backends/aspnetcore/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| `Kizamu.Backend.AspNetCore.Api.Tests/ApiDependenciesTests.cs`、`Kizamu.Backend.AspNetCore.Application.Tests/ApplicationDependenciesTests.cs`、`Kizamu.Backend.AspNetCore.Infrastructure.Tests/InfrastructureDependenciesTests.cs` | 維持。参照可能なアセンブリは各層で異なる。許可する依存先をテスト本文から読める。 |
| [Kizamu.Backend.AspNetCore.Api.Tests/BlazorHostingTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/BlazorHostingTests.cs)、[Kizamu.Backend.AspNetCore.Api.Tests/HealthEndpointTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/HealthEndpointTests.cs) | 修正。テストごとのHTTPクライアントを生成するフィクスチャと、そのクライアントを破棄する責任をクラス内で直接表す。 |
| `Kizamu.Backend.AspNetCore.Api.Tests/Kizamu.Backend.AspNetCore.Api.Tests.csproj`、`Kizamu.Backend.AspNetCore.Application.Tests/Kizamu.Backend.AspNetCore.Application.Tests.csproj`、`Kizamu.Backend.AspNetCore.Infrastructure.Tests/Kizamu.Backend.AspNetCore.Infrastructure.Tests.csproj` | 維持。各プロジェクトに必要な対象フレームワーク、参照方向、テスト依存を宣言している。 |
| [Kizamu.Backend.AspNetCore.Api.Tests/TimeRecordEndpointTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/TimeRecordEndpointTests.cs) | 修正。拒否後の一覧GET・200・JSON読取を再利用し、開始・終了を拒否した後に何が保存されているかを本文で確認する。 |
| `Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/FakeTimeRecordState.cs`、`Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordEndOperationTests.cs`、`Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordReaderTests.cs`、`Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordStartOperationTests.cs`、`Kizamu.Backend.AspNetCore.Infrastructure.Tests/TimeRecords/InMemoryTimeRecordStateTests.cs` | 維持。ID指定の終了、保存順・列挙中の追加・同一参照、時計を内側で確認。テストの操作順と準備を保持。 |

### Nodeテスト（`tests/backends/node/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| `eslint.config.js` | 維持。各層の対象・禁止依存・環境を明示。設定を共有する範囲は既存のまま。 |
| `healthEndpoint.test.ts` | 維持。正常性HTTP応答のJSONとContent-Typeの検証に限定。 |
| `kizamu-backend-node-tests.esproj`、`package.json`、`tsconfig.json`、`vitest.config.ts` | 維持。実行入口、型検査、依存先またはテスト起動を宣言。構成の差はツールや層に対応。 |
| [serverStartup.test.ts](../tests/backends/node/serverStartup.test.ts) | 修正。待受URLの取得・タイムアウトとプロセスの停止・close待ちを分け、テスト本文は起動したAPIへの要求と期待応答を示す。 |
| [timeRecordEndpoints.test.ts](../tests/backends/node/timeRecordEndpoints.test.ts) | 修正。拒否後の一覧GET・200・JSON読取を再利用し、開始・終了を拒否した後に何が保存されているかを本文で確認する。 |
| `timeRecordInfrastructure.test.ts`、`timeRecords.test.ts` | 維持。記録源と保存順は別の公開境界。未定義の戻り値、記録のクラス、順序を含む期待を照合。 |

### Blazorテスト（`tests/frontends/blazor/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| [Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs)、[Kizamu.Frontend.Blazor.Presentation.Tests/Projects/ProjectTabsViewModelTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/Projects/ProjectTabsViewModelTests.cs) | 修正。タブの準備に既存のViewModelを使い、登録・キャンセルの状態仕様は内側で確認する。画面は入力・クリックと選択状態の表示を確認する。 |
| [Kizamu.Frontend.Blazor.Client.Tests/Health/HealthStatusTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/Health/HealthStatusTests.cs)、[Kizamu.Frontend.Blazor.Client.Tests/Kizamu.Frontend.Blazor.Client.Tests.csproj](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/Kizamu.Frontend.Blazor.Client.Tests.csproj)、[Kizamu.Frontend.Blazor.Presentation.Tests/Kizamu.Frontend.Blazor.Presentation.Tests.csproj](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/Kizamu.Frontend.Blazor.Presentation.Tests.csproj) | 修正。接続状態はViewModel、HTTPはサービス、Razorは日本語表示と初期化を担当。画面試験の準備をサービス境界に揃えた。 |
| [Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecordTimesTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecordTimesTests.cs)、[Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecordTimesFormatterTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecordTimesFormatterTests.cs) | 修正。日本時間への変換・経過時間の計算を直接テストし、HTMLの時刻属性とタイマーの再描画・停止を画面で確認する。 |
| [Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecorderTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecorderTests.cs) | 修正。操作の可否と失敗通知をViewModelで決め、一覧取得中の更新・終了対象・失敗時の再取得を各操作から読めるよう分けた。画面は表示と入力を接続する。 |
| `Kizamu.Frontend.Blazor.Infrastructure.Tests/Health/HealthApiTests.cs` | 維持。正常性の通信判定はHealthApi。確認中・成功・失敗の判定が画面とHTTPの結合テストに残る。 |
| `Kizamu.Frontend.Blazor.Infrastructure.Tests/InfrastructureDependenciesTests.cs`、`Kizamu.Frontend.Blazor.Presentation.Tests/PresentationDependenciesTests.cs` | 維持。参照可能なアセンブリは各層で異なる。許可する依存先をテスト本文から読める。 |
| `Kizamu.Frontend.Blazor.Infrastructure.Tests/Kizamu.Frontend.Blazor.Infrastructure.Tests.csproj`、`Kizamu.Frontend.Blazor.Server.Tests/Kizamu.Frontend.Blazor.Server.Tests.csproj` | 維持。各プロジェクトに必要な対象フレームワーク、参照方向、テスト依存を宣言している。 |
| `Kizamu.Frontend.Blazor.Infrastructure.Tests/TimeRecords/TimeRecordApiTests.cs` | 維持。HTTP応答の破棄とDomainへの変換を通信境界で完結。成功・失敗・JSONを直接確認。 |
| [Kizamu.Frontend.Blazor.Presentation.Tests/Health/HealthStatusViewModelTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/Health/HealthStatusViewModelTests.cs)、[TestSupport/StubHealthService.cs](../tests/frontends/blazor/TestSupport/StubHealthService.cs) | 修正。応答待ち・正常・異常・通信失敗をHTTPや画面なしで再現し、接続状態を直接検証する。 |
| [Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs) | 修正。遅延一覧が届く順、操作の失敗と再取得、選択中のプロジェクトと終了対象の関係をViewModelの公開操作から直接検証する。 二つのプロジェクトが記録中という前提を名前付きの準備にまとめた。 |
| `Kizamu.Frontend.Blazor.Server.Tests/Hosting/ApiProxyTests.cs` | 維持。転送先・HTTPメソッド・クエリ・本文・応答が同じ転送の往復を保証。 |
| [Kizamu.Frontend.Blazor.Server.Tests/Hosting/BlazorHostingTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Server.Tests/Hosting/BlazorHostingTests.cs) | 修正。テストごとのHTTPクライアントを生成するフィクスチャと、そのクライアントを破棄する責任をクラス内で直接表す。 |
| [TestSupport/StubTimeRecordService.cs](../tests/frontends/blazor/TestSupport/StubTimeRecordService.cs) | 修正。テストが開始・終了応答を保留できるようにし、応答を成功・失敗させる時点と操作順を個別のテストに残す。 |
| `TestSupport/TestHttpMessageHandler.cs` | 維持。通信を介さず遅延応答・要求回数・IDを観測するスタブと、HTTP要求を記録するハンドラーを区別。 |

### Reactテスト（`tests/frontends/react/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| [App.test.tsx](../tests/frontends/react/App.test.tsx)、[presentation/projectTabsState.test.ts](../tests/frontends/react/presentation/projectTabsState.test.ts) | 修正。登録、キャンセル、選択をReducerとその直接テストに置き、入力・クリック・タブ表示の接続はAppで確かめる。 |
| `architecture/eslintBoundaries.test.ts`、`eslint.config.js` | 維持。Presentationの依存制約と禁止・許可の入力例、テストが使うLint設定を照合。 |
| `health/HealthStatus.test.tsx`、`presentation/useHealthStatus.test.ts` | 維持。正常性の判定・非同期応答と、通知領域の日本語表示が分離されている。 |
| [infrastructure/healthService.test.ts](../tests/frontends/react/infrastructure/healthService.test.ts) | 修正。型付きfetchと標準のJSON応答を使い、正常・異常・statusの内容が異なる場合を通信境界で検証する。 |
| [infrastructure/timeRecordService.test.ts](../tests/frontends/react/infrastructure/timeRecordService.test.ts) | 修正。開始・終了のPOSTとDTO変換を共有し、通信テストは型付きfetchと標準JSON応答で経路・失敗・Domain変換を保証する。 |
| `kizamu-frontend-react-tests.esproj`、`package.json`、`tsconfig.json`、`vitest.config.ts` | 維持。起動HTML、依存組立、Vite・JSX・Vitest・Visual Studioの各入口と対象を確認。 |
| [presentation/timeRecorderOperationState.test.ts](../tests/frontends/react/presentation/timeRecorderOperationState.test.ts)、[timeRecords/TimeRecorder.test.tsx](../tests/frontends/react/timeRecords/TimeRecorder.test.tsx) | 修正。操作の排他・再取得と、通知の優先順位をReducer・フックで表現。画面からの入力と状態別の表示を確かめる。 |
| [presentation/timeRecorderState.test.ts](../tests/frontends/react/presentation/timeRecorderState.test.ts) | 修正。取得中の更新を一覧へ反映する順序と、終了対象の決定を別々に読める。重複ID、順序、参照、ISO表記を直接検証した。 |
| `presentation/useTimeRecordTimes.test.ts`、`timeRecords/TimeRecordTimes.test.tsx` | 維持。Domainと元のISO文字列、タイマーの寿命、表示用時刻、HTML属性の保証範囲を照合。 |
| [presentation/useTimeRecorder.test.ts](../tests/frontends/react/presentation/useTimeRecorder.test.ts) | 修正。共通フィクスチャを保留・失敗応答にも使い、操作順とエラー本体を各テストに残す。先行する未コミットの4差分は別途保全した。 |
| `test/createTimeRecordServiceStub.ts`、`test/deferred.ts`、`test/setup.ts`、`test/timeRecords.ts` | 維持。既定サービス・遅延応答・DOMのshould登録と後始末・日時を伴うDomainデータに役割を限定。 |

### 共通テスト補助（`tests/`）

| ファイル | 全行精査の結果 |
| --- | --- |
| `TestSupport/TestCancellation.cs`、`should.mts` | 維持。xUnitのキャンセルとVitestのshouldを、それぞれテスト基盤の一か所に置く。 |
