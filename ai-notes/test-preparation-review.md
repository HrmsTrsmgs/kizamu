# テスト準備の可読性の精査

## 目的と状態

2026-11-10の開発者指示に基づき、テストの準備の詳細が、確認する操作と期待結果を埋もれさせていないかを全件確認した。方針の正本は [テストの準備と補助メソッド](../docs/testing-style.md#テストの準備と補助メソッド)。抽出候補は提案であり、個々のテストの修正や既存のSkip解除を了承済みとは扱わない。

初回は方針の明文化とコードの精査を行った。その後の追加精査と適用状況は次節に記載する。精査だけで製品全体の正しさを保証するものではない。

## C#の共通準備と個別準備の再精査（2026-11-11）

基準は `codex/refactor-react-test-preparation` の `dd1785a24f2cd327f53f0692a9e72b0589d2e54b` と、作業中だったAppTests.csのキャンセル用タプル抽出。代表例1件で止まっていたため、Git追跡下のC#全25テストファイルと関連する補助コードを確認し、共通初期化と個別の準備を再精査した。Fact 73件・Theory 11件の計84定義（うちSkip 3定義）で、データ行の展開数ではない。

| 領域 | 確認ファイル数 | テスト定義数 | 整理したファイル数 |
| --- | ---: | ---: | ---: |
| Backend API | 4 | 6 | 1 |
| Backend Application | 4 | 10 | 2 |
| Backend Infrastructure | 2 | 8 | 1 |
| Domain | 3 | 6 | 0 |
| Blazor Client | 4 | 22 | 1 |
| Blazor Infrastructure | 3 | 11 | 0 |
| Blazor Presentation | 3 | 18 | 2 |
| Blazor Server | 2 | 3 | 0 |
| 合計 | 25 | 84 | 7 |

整理した7ファイルは以下のとおり。共通準備をコンストラクターへまとめ、個別の準備に複数の結果が必要なときだけ名前付きタプルで返す。結果が一つなら単一の戻り値を使い、共通フィールドだけで操作・検証できる準備は戻り値を増やさない。

| ファイル | 整理した内容 |
| --- | --- |
| [TimeRecordEndpointTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/TimeRecordEndpointTests.cs) | 一覧取得前の時計設定と終了要求を抽出。終了応答を単一値で返し、using・200確認・GET応答より後の破棄を本文に維持。 |
| [TimeRecordEndOperationTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordEndOperationTests.cs) | 空のstateを共通化。開始時刻の異なる二件と、終了済み記録を含む二件の準備から、検証に使う記録二つをタプルで返す。生成・終了・保存の順序を維持。 |
| [TimeRecordReaderTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordReaderTests.cs) | stateとtestedを共通化。記録中・終了済みの混在状態から記録二つをタプルで返す。列挙前・列挙中の追加と列挙子の破棄は本文に維持。 |
| [InMemoryTimeRecordStateTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Infrastructure.Tests/TimeRecords/InMemoryTimeRecordStateTests.cs) | testedの反復生成をコンストラクターへ集約。追加・コピー・列挙の操作は本文に維持。 |
| [AppTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs) | キャンセル用の画面・ポップアップのタプル抽出を保持。Skipのタブ切替テストでは記録中画面の準備を単一戻り値で抽出。 |
| [ProjectTabsViewModelTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/Projects/ProjectTabsViewModelTests.cs) | testedを共通化。再表示テストの先行登録は戻り値なしの準備へ抽出。 |
| [TimeRecorderViewModelTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs) | 共通フィールドを使う履歴・記録中状態の準備はTaskの補助メソッドへ抽出。Skipの二対象終了テストで固有に生成するtestedとserviceはタプルで返す。識別子の分岐、対象選択、応答競合の順序は本文に維持。 |

残る18ファイルも確認した。依存境界の6ファイルは依存一覧の取得が検証対象。Domainの2ファイルと開始操作の1ファイルは、生成・復元・開始と時刻がそれぞれ対象操作または入力条件である。ホスト・正常性・中継の4ファイル、Blazorの表示3ファイルと通信2ファイルは、コンストラクターや既存補助メソッドに生成・接続を集約済みで、残る応答内容・時計・描画・要求と破棄の順序を本文で追える構成を維持した。

検証対象は基準HEADと7テストファイルの差分。テスト名・属性・データ指定・Skip理由・全アサーション文はHEADとの機械比較で一致した。Kizamu.Blazor.slnxの非増分build・解析は警告0・エラー0で成功、全.NET testは成功94・失敗0・既存Skip3（計97）。対象7ファイルのformat検査は既存SkipへのxUnit1004 info 3件で終了2。SARIFには同3件と対象外Program.csのASP0027 note 1件が残り、新しい補助メソッド・コンストラクターの診断はない。差分検査とUTF-8 BOMなし・CRLFを確認した。

Skipは解除しておらず、その本体から抽出した準備の動作は実行検証されていない。製品コード・構成・TypeScriptは変更していないためnpm検証は再実行していない。開発者の差分確認とコミット了承を得て、このコミットに保存する。現在の停止位置は [現在の次作業](mvp-implementation-plan.md#現在の次作業) を参照する。

## beforeEachによる共通準備の追加精査（2026-11-11）

基準は `codex/refactor-react-test-preparation` の `892aa7b6fc31508ebb85931f469137ad25eb5a01`。この追加確認の前に、今回の `beforeEach` 方針で確認・修正していたのは `useTimeRecorder.test.ts` だけだった。今回はGit追跡下のTypeScriptテスト全19ファイルを構文解析で列挙し、64件のテスト定義と関連する準備コードを読んだ。`it.each` のデータ行は展開せずに数えている。C#のコンストラクターへの共通化は、この追加精査の対象に含めていない。

開発者の変更指示を受け、次の5ファイルの共通準備を `beforeEach` へ集約した。開発者の差分確認とコミット了承を得て、このコミットに保存する。

| 対象 | テスト定義数 | 共通準備へまとめた内容 | 本文へ残す条件・操作 |
| --- | ---: | --- | --- |
| [healthService.test.ts](../tests/frontends/react/infrastructure/healthService.test.ts) | 4 | fetchモックの生成とグローバルfetchの差し替え。既存のafterEachで差し替えを解除する。 | HTTPステータス、応答内容、正常性確認の呼び出しと期待値。 |
| [timeRecordService.test.ts](../tests/frontends/react/infrastructure/timeRecordService.test.ts) | 8 | fetchモックの生成とグローバルfetchの差し替え。応答を作る既存関数は利用する。 | 一覧・開始・終了それぞれのDTO、日時表記、呼び出すAPIと変換結果。 |
| [useHealthStatus.test.ts](../tests/frontends/react/presentation/useHealthStatus.test.ts) | 4 | 接続確認用のモックと依存オブジェクトの生成。フックは条件設定後に生成する。 | 未応答・成功・失敗、旧応答の保留と完了、新しい接続先への切替順序とオブジェクトの区別。 |
| [TimeRecorder.test.tsx](../tests/frontends/react/timeRecords/TimeRecorder.test.tsx) | 3 | 一覧・開始・終了のモックとサービスStubの生成。画面は個別条件の設定後に描画する。 | 一覧に返す記録、画面の描画、開始・終了・再開始の操作と表示の期待値。再開始の仕様分離は既存候補C1として扱う。 |
| [NodeのtimeRecords.test.ts](../tests/backends/node/timeRecords.test.ts) | 3 | 空の状態Fake、ID生成カウンター、時計、TimeRecordsの接続に共通する初期化。 | 保存済み記録、IDと時刻の条件、二件の開始順、終了対象、再終了時刻。 |

NodeのB2で検討した開始済み状態の補助関数とは分け、今回はFake・時刻・ID生成の共通初期化を対象にする。準備の短さだけで判断せず、共有する初期化と各テストの条件を分けられるかで検討する。ファイル全体の行数の増減は評価基準にしない。

残る14ファイルも確認した。判定の内訳は次のとおり。

* 既に共通化済みの3ファイル：NodeのHTTPテスト、`useTimeRecordTimes`、`useTimeRecorder`。アプリ・時計・モックをテストごとに用意している。フック生成前の時刻設定や、開始・応答の順序は本文へ維持する。
* 依存境界テスト2ファイル：ESLintの設定読み込みは既存の `beforeAll` に集約済みで、各本文は異なる検査対象のコードを与えている。
* テスト定義が一つの5ファイル：Nodeの正常性確認・起動入口、Domainの期間計算、ReactのApp・時刻表示。ファイル内で繰り返す共通初期化はない。Node起動の待機・後始末は既存の補助関数にあり、プロセス起動と描画・計算などの対象操作は本文へ残す。
* その他4ファイル：Domainの記録、NodeのInfrastructure、Reactの接続状態表示・Reducer。開始と復元、状態保存と生成源、未応答と応答、イベントごとの元状態は、それぞれ検証対象または個別の入力条件である。共通の付帯初期化を追加で移す対象は確認できなかった。

変更後版で全81テスト・Lint・buildが成功し、テスト名とアサーション行の維持、UTF-8 BOMなし・CRLF、差分検査を確認した。次の作業単位は [現在の次作業](mvp-implementation-plan.md#現在の次作業) を参照する。以下は初回精査時の収集版と候補であり、この追加確認の基準コミットとは異なる。

## 初回精査の対象版と確認範囲（2026-11-10）

2026-11-10 10:59 JSTに保存したファイル内容を対象とした。この作業ツリーだけではNode・Blazor等の新しい構成が含まれないため、他の作業版も読み取りだけで確認した。

| 略号 | 対象版 | テストファイル数 | テスト定義数 |
| --- | --- | ---: | ---: |
| M | この作業ツリー。HEAD `66325ca3922093a6ff8331c5328c55b6471d5d70` | 13 | 54 |
| R | `codex/fix-end-time-record-id` の `d67ec9fc746f6961565a44f3e991955e9ead6093` と、その作業版のテスト差分 | 42 | 136 |
| P | `codex/project-tabs` の `545d000711e35e3eceeb8ff5a8a08f7ad4a31dab` と、その作業版のテスト差分 | 44 | 135 |

* 計99ファイル分を収集し、内容が同一のものをまとめた66種類・227件のテスト定義をすべて読んだ。Theoryや`it.each`のデータ行は個別に数えていない。Skipの本体も対象に含めた。
* テスト本体に加え、Fake・Stub、時刻・表示データの生成、遅延応答、初期化等の補助コード14種類を確認した。個人設定、生成物、依存パッケージのソースは対象外。
* Rの収集時差分はNodeの `timeRecords.test.ts`、Blazorの `TimeRecorderViewModelTests.cs`、Reactの `useTimeRecorder.test.ts`。Pの収集時差分はBlazorの `TimeRecorderViewModelTests.cs` と `StubTimeRecordService.cs`。これらをコミット版だけの状態と混同しない。
* 同じファイルでもR・Pで内容が異なる場合は別々に確認した。本文中の行番号はこの保存版のもの。SHA-256、本文、対応表はローカルの無視対象 `.artifacts/test-preparation-audit/` に保持している。未コミットの他作業版そのものは、この記録のコミットには含まれない。
* テスト定義の有無と補助コードを機械的に列挙してから、各本文を読んだ。長さだけで抽出の要否は決めていない。

## 先に扱うと効果が大きい候補

### A1：プロキシテストのサーバー設定

対象はR・Pの `tests/frontends/blazor/Kizamu.Frontend.Blazor.Server.Tests/Hosting/ApiProxyTests.cs:24`。一つのTheoryの本文に、接続先設定、DIの差し替え、FactoryとClientの生成が12行続く。ここは中継の結果を検証するための準備で、要求の送信と検証を読み始めるまでに基盤設定を読む必要がある。

記録用バックエンドと接続先を引数で受け、設定済みのFactoryを返す補助メソッドへ抽出する案がある。一か所だけの利用でも効果がある。HTTPメソッド・パス・要求本文・応答ステータスはTheoryの引数と本文に残し、要求の送信と中継結果の検証も本文に残す。Factory、Client、Request、Response、Backendの破棄範囲を維持する。

### A2：Node起動テストの出力監視と後始末

対象はR・Pの `tests/backends/node/serverStartup.test.ts:12`。起動URLを受け取るまでの標準出力・標準エラーの蓄積、イベント購読、タイムアウト、終了待機が本文の大部分を占める。一つのテストだけで使う処理だが、起動した入口にHTTPで接続する検証が埋もれている。

起動URLを待つ処理とプロセスの後始末を、ファイル内の意味のある補助関数へ分ける案がある。実行する入口、空きポートの指定、プロセス起動、受け取ったURLへの要求、期待する応答は本文で追える形にする。エラー時の出力情報、タイムアウト解除、killとcloseの待機を維持する。

### A3：NodeのHTTPテストの開始・終了済み記録の準備

対象はR・Pの `tests/backends/node/timeRecordEndpoints.test.ts:38`、`:62`、`:79`、`:94`。終了・一覧・アプリ間の分離を確認する前に、開始要求、開始の成功確認、JSONの取り出しを繰り返している。終了済み一覧のテストでは終了要求とその成功確認も準備に含まれる。

「開始済みの記録を用意する」「終了済みの記録を用意する」単位で抽出する案がある。アプリと時刻を呼び出し側から指定し、二件の記録の順序・識別子の関係は本文から分かるようにする。開始API自体のテストは、開始要求と期待結果を直接記述する。別アプリを生成する操作も、分離のテストの主題として本文に残す。

### A4：ASP.NETの一覧テストの終了済み記録の準備

対象はR・Pの `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/TimeRecordEndpointTests.cs:64`。開始済み記録を用意する処理は抽出されているが、一覧取得の前提となる終了要求・成功確認が本文に残っている。Mの `tests/Kizamu.Server.Tests/TimeRecordEndpointTests.cs:101` も同じ構造。

終了済み記録を用意する補助メソッドへまとめる案がある。開始時刻と終了時刻を引数へ残し、一覧取得とその応答検証が続けて読めるようにする。終了API自体のテストには終了要求と期待結果を残す。MとR・Pを別々に修正せず、実際に適用する基準版を決めてから扱う。

### A5：Blazorのキャンセルテストの入力済みポップアップ

対象はPの `tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs:53`。キャンセルを確認する前に、画面生成、追加ボタンの検索・クリック、ポップアップと入力欄の検索、入力欄の存在確認、名前入力が並ぶ。

入力済みの追加ポップアップを用意する補助メソッドへ抽出し、名前を引数にする案がある。キャンセル操作と、未登録・ポップアップ非表示の検証を本文に残す。登録操作自体を確認する同ファイル`:67`では、追加から登録までの主題の操作を隠さない。既存の `プロジェクトを登録する` は登録済み状態を作る準備として使われており、そこへキャンセルまで扱う汎用分岐は追加しない。

## 小さな単位で検討する候補

### B1：Applicationの保存済み状態の組み立て

対象はRの `TimeRecordEndOperationTests.cs:26`、`:38`、`:53`、`:69` と `TimeRecordReaderTests.cs:23`、Pの対応する `EndTimeRecordTests.cs:27`、`GetTimeRecordsTests.cs:23`。いずれも `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/` 配下。

「渡された記録を保存した状態を用意する」補助メソッドで、Fakeの生成とAddの列をまとめる案がある。対象の記録、他の記録、開始・終了時刻は本文に残す。特にReaderの列挙前・列挙中の追加は検証対象なので、これらのテストのAddを一括して準備へ移さない。InfrastructureでAdd自体を確認するテストも同様。

### B2：Nodeの終了テストで開始済み状態を用意する手順

対象はRの `tests/backends/node/timeRecords.test.ts:36`、Pの同ファイル`:37`。一件目だけを終了する検証の前に、識別子生成、可変時計、二回の開始、時計の変更が並ぶ。

開始済み二件の準備を意味のある関数で表す案がある。順序、終了対象、返す時刻は呼び出し側から分かるようにする。開始と一覧を確認する先頭テストでは、生成源と開始操作が主題なので本文に残す。Rの再終了テストは既に `createState(records)` で前提を指定でき、同じ長い準備はない。

### B3：Reactの記録中フックの準備

対象はRの `tests/frontends/react/presentation/useTimeRecorder.test.ts:83`、`:99`、Pの同ファイル`:43`、`:59`。終了・再開始を確認する前のStub構築、フック生成、最初の開始・終了を、記録中または終了済みの状態を用意する補助関数へまとめる案がある。監視するサービス関数や記録データを引数へ残す。

Rの遅延一覧テスト`:44`と再読み込みテスト`:66`も確認した。Stubの組み立てだけは抽出候補だが、一覧要求・開始・旧応答の解決・再読み込みの順序は本文に残す。フックへ渡すサービスのインスタンスを、補助関数化で再描画のたびに作り直さない。Rではこの領域がレビュー対応中なので、他の作業で同時編集しない。

### B4：Blazorの対象別・履歴付きの準備

対象はPの `tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs:82`。未指定とプロジェクトの両方で記録中の状態を作る手順は、終了する対象・残る記録を引数から読める単位で抽出できる。収集版ではSkip中なので、今の段階でその本体を整理する許可とは扱わない。

同ファイル`:151`、Rの対応箇所`:104`の、過去の履歴を読み込んで一件開始する準備も候補。ただし二～三行なので優先度は低い。対象を切り替えて戻るテストや、開始応答の前に対象を切り替えるテストでは、切替・開始・応答の順序が主題であり、その列は本文に残す。Rの遅延一覧テストも同じ理由で順序を隠さない。

## 準備の抽出だけでは解決しない長さ

### C1：Reactの画面テストに再開始の検証まで含まれる

適用状況（2026-11-11）：基準HEAD `5665d4b` の作業差分で、再開始操作と操作欄の終了時刻の不在確認を独立したテストへ移した。開始・終了の既存アサーションと操作順序は元のテストに残し、再開始側の画面準備は終了時刻の表示まで待つ補助関数へ集約した。共通依存はbeforeEachに維持し、fixture・製品コード・設定は変更していない。検証結果とコミット状況は [現在の次作業](mvp-implementation-plan.md#現在の次作業) を参照する。以下は初回精査時の指摘。

対象はR・Pの `tests/frontends/react/timeRecords/TimeRecorder.test.tsx:54`。準備は四行程度だが、開始、終了、再開始と各段階の検証が一つの本文に並ぶ。開始・終了の代表的な接続確認は一連のテストとして読める。一方、`:80`以降の「再開始すると終了時刻が消える」は別の仕様として読めるため、開始・終了済みの画面を用意して確認する独立テストへ分ける案がある。

期待結果を補助メソッドへ隠して行数だけを減らす整理はしない。分ける場合も、現在のアサーションを移す対応関係を確認し、削除やViewModelテストへの単純な置き換えは行わない。

## 旧構成で確認した候補

Mの `src/Kizamu.Client/src/timeRecords/TimeRecorder.test.tsx` は、一覧用データ・通信Stubの構築、開始済み画面の準備、偽時計の設定と後始末が本文を長くしている。特に`:262`、`:281`、`:304`、`:327`、`:349`は終了・再開始の前提が長い。記録データ、開始済み画面、時計の準備を分ける候補がある。

ただしR・Pでは表示・状態・通信に既に分離され、補助関数も導入されている。Mだけに大きな整理を適用すると別作業と重複するため、旧版の指摘として残す。MのApplicationのStart・Endの共通初期化も新しい版では構成が変わっており、旧版だけの整理を先行しない。

## 代表例の変更案

A5のキャンセルテストを代表例にする案。次の補助メソッドは新規提案であり、既存APIや実装済みの変更ではない。

変更前の準備と操作：

```csharp
var tested = context.Render<Client.App>();
tested.FindAll("button").Single(it => it.TextContent == "追加").Click();
var popup = tested.Find("[role=dialog]");
var nameInput = popup.QuerySelector("input");
nameInput.Should().NotBeNull();
nameInput.Change("プロジェクトA");
popup.QuerySelectorAll("button").Single(it => it.TextContent == "キャンセル").Click();
```

変更後の本文案：

```csharp
var tested = context.Render<Client.App>();
var popup = 名前を入力した追加ポップアップを用意する(tested, "プロジェクトA");

popup.QuerySelectorAll("button").Single(it => it.TextContent == "キャンセル").Click();

tested.FindAll("[role=dialog]").Should().BeEmpty();
tested.FindAll("[role=tab]").Select(it => it.TextContent).Should().NotContain("プロジェクトA");
```

入力欄の存在確認は準備の成立確認として補助メソッドへ移し、キャンセル後の期待結果は本文に残す。名前入力までの具体的な検索と操作を補助メソッドへ移すだけで、評価順序・テスト名・Skipは変えない。プロジェクト機能の作業版との統合と、この代表例の読みやすさを確認してから適用範囲を決める。

## 全ファイルの確認結果

各行の定義数はテスト関数・メソッドの数であり、データ駆動の実行ケース数ではない。「維持」は今回の準備の可読性という観点での判断で、機能の正しさの保証ではない。同じ内容のファイルは一行にまとめ、版が異なるものは別行にした。

### 旧構成M

| 番号 | 版 | ファイル | 定義数 | 確認結果 |
| --- | --- | --- | ---: | --- |
| 001 | M | `src/Kizamu.Client/src/App.test.tsx` | 1 | 維持。二つのStubと画面生成だけで前提を読める。 |
| 002 | M | `src/Kizamu.Client/src/health/HealthStatus.test.tsx` | 2 | 維持。正常・通信失敗の条件を短いStubで直接指定している。 |
| 003 | M | `src/Kizamu.Client/src/health/healthApi.test.ts` | 4 | 維持。JSON生成はresponseへ抽出済み。ステータスと内容は本文の条件。 |
| 006 | M | `src/Kizamu.Client/src/timeRecords/TimeRecorder.test.tsx` | 17 | 旧構成の候補。通信データ、開始済み画面、偽時計の準備が長い。旧構成の節を参照。 |
| 007 | M | `src/Kizamu.Client/src/timeRecords/timeRecordApi.test.ts` | 6 | 維持。JSON生成はresponseへ抽出済み。要求先と応答データは各仕様を読む条件。 |
| 008 | M | `src/Kizamu.Client/src/timeRecords/timeRecordDuration.test.ts` | 1 | 維持。二つの日時点から期間を検証する短い本文。 |
| 009 | M | `src/Kizamu.Client/src/timeRecords/timeRecorderState.test.ts` | 4 | 維持。Reducerへ渡す既存状態は結果を左右する条件で、各本文から読める。 |
| 010 | M | `tests/Kizamu.Application.Tests/TimeRecords/EndTimeRecordTests.cs` | 3 | 旧構成の候補。三件に共通する対象生成はまとめられる。開始済み記録は抽出済み。 |
| 012 | M | `tests/Kizamu.Application.Tests/TimeRecords/StartTimeRecordTests.cs` | 3 | 旧構成の候補。三件の対象生成は共通初期化へまとめられる。新構成と重複修正しない。 |
| 013 | M | `tests/Kizamu.Domain.Tests/TimeRecordTests.cs` | 3 | 維持。開始・終了と期待結果を直接読める短い本文。 |
| 014 | M | `tests/Kizamu.Infrastructure.Tests/TimeRecords/InMemoryTimeRecordStateTests.cs` | 4 | 維持。Addと取得順の確認が主題。記録追加の列を準備として隠さない。 |
| 015 | M | `tests/Kizamu.Server.Tests/HealthEndpointTests.cs` | 1 | 維持。Client生成はコンストラクターへ分離済み。HTTP要求と応答検証を本文に残す。 |
| 016 | M | `tests/Kizamu.Server.Tests/TimeRecordEndpointTests.cs` | 5 | 候補A4。終了済み一覧の準備を抽出可能。開始・終了API自体の操作は本文に残す。 |

### 新構成Rと同内容のP

| 番号 | 版 | ファイル | 定義数 | 確認結果 |
| --- | --- | --- | ---: | --- |
| 018 | R・P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/ApiDependenciesTests.cs` | 1 | 維持。参照先の絞り込みと期待値自体が検証対象で、準備の膨張はない。 |
| 019 | R・P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/BlazorHostingTests.cs` | 1 | 維持。Factoryはクラスの初期化にあり、配信しないパスはデータ行から読める。 |
| 020 | R・P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/HealthEndpointTests.cs` | 1 | 維持。Client生成は分離済み。HTTP要求と応答検証の短い本文。 |
| 021 | R・P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/TimeRecordEndpointTests.cs` | 3 | 候補A4。終了済み一覧の準備だけを追加抽出できる。既存の開始準備は維持。 |
| 022 | R | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/ApplicationDependenciesTests.cs` | 1 | 維持。参照一覧の取得は一行。禁止・許可する依存先を本文で直接検証する。 |
| 024 | R | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordEndOperationTests.cs` | 4 | 候補B1。状態の生成とAddの反復をまとめ、対象の記録と時刻は本文に残す。 |
| 025 | R | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordReaderTests.cs` | 4 | 一部候補B1。保存済み一覧の準備を抽出可能。列挙開始前・途中の追加は本文維持。 |
| 026 | R | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordStartOperationTests.cs` | 1 | 維持。状態と対象の生成は二行。時刻の引数と保存結果を直接読める。 |
| 027 | R・P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Infrastructure.Tests/InfrastructureDependenciesTests.cs` | 1 | 維持。参照先の取得と絞り込み自体が検証対象。 |
| 028 | R | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Infrastructure.Tests/TimeRecords/InMemoryTimeRecordStateTests.cs` | 7 | 維持。検索用の二件の記録は短い準備。追加・コピー・列挙の時点は各仕様の主題。 |
| 029 | R・P | `tests/backends/node/healthEndpoint.test.ts` | 1 | 維持。生成は一行。HTTP要求と期待結果、アプリのcloseが明確。 |
| 030 | R・P | `tests/backends/node/serverStartup.test.ts` | 1 | 候補A2。出力監視・タイムアウト・後始末が一件の本文を占める。 |
| 031 | R・P | `tests/backends/node/timeRecordEndpoints.test.ts` | 6 | 候補A3。終了・一覧・別アプリのテストで開始成功確認とJSON取得が反復している。 |
| 032 | R・P | `tests/backends/node/timeRecordInfrastructure.test.ts` | 2 | 維持。短い二件。置換前の二記録と時計条件は直接読め、タイマーをfinallyで戻す。 |
| 033 | R | `tests/backends/node/timeRecords.test.ts` | 3 | 一部候補B2。終了テストの二件の開始と時計切替をまとめる余地がある。 |
| 034 | R・P | `tests/domains/dotnet/Kizamu.Domain.Tests/DomainDependenciesTests.cs` | 1 | 維持。参照一覧の取得は一行。禁止する依存の検証を本文に残す。 |
| 035 | R・P | `tests/domains/dotnet/Kizamu.Domain.Tests/TimeRecordTests.cs` | 4 | 維持。生成・復元・終了の入力条件を直接指定している。 |
| 036 | R・P | `tests/domains/typescript/dependencyBoundaries.test.ts` | 3 | 維持。Lintの初期化はbeforeAllへ分離済み。依存コード例と期待診断が本文の主題。 |
| 037 | R・P | `tests/domains/typescript/timeRecord.test.ts` | 3 | 維持。開始・復元・終了と元の不変性を直接読める。準備は一～二行。 |
| 038 | R・P | `tests/domains/typescript/timeRecordDuration.test.ts` | 1 | 維持。日時点の準備は短く、期間の期待値が直接読める。 |
| 039 | R | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs` | 1 | 維持。通信・DIの共通構築はコンストラクターにあり、本文は画面生成と検証だけ。 |
| 040 | R・P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/Health/HealthStatusTests.cs` | 4 | 維持。各応答条件は一～四行で読める。待機する応答と例外を隠す必要はない。 |
| 041 | R・P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecordTimesTests.cs` | 8 | 維持。表示するへ準備を抽出済み。時計の境界、再描画、終了の順序は本文に残す。 |
| 042 | R・P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecorderTests.cs` | 5 | 維持。終了テストは開始するで記録中の画面を準備済み。再開始の操作順序も読める。 |
| 043 | R・P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure.Tests/Health/HealthApiTests.cs` | 4 | 維持。応答するでJSON応答を準備済み。異常ステータスと内容は条件として本文に残す。 |
| 044 | R・P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure.Tests/InfrastructureDependenciesTests.cs` | 1 | 維持。参照先の取得と期待する層の検証が本文の主題。 |
| 045 | R・P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure.Tests/TimeRecords/TimeRecordApiTests.cs` | 6 | 維持。応答するへ通信の準備を抽出済み。DTOの値と対応するDomainの検証を読める。 |
| 046 | R・P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/PresentationDependenciesTests.cs` | 1 | 維持。参照一覧の取得は一行。許可・禁止する依存先を直接検証する。 |
| 047 | R | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs` | 10 | 一部候補B4。履歴付き開始の準備はまとめられる。遅延応答と再読み込みの順序は維持。 |
| 048 | R・P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Server.Tests/Hosting/ApiProxyTests.cs` | 1 | 候補A1。一件のTheory内に接続先設定・依存差し替え・Factory生成が並ぶ。 |
| 049 | R・P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Server.Tests/Hosting/BlazorHostingTests.cs` | 2 | 維持。Client生成はクラスの初期化へ分離済み。配信パスと応答を直接読める。 |
| 052 | R・P | `tests/frontends/react/App.test.tsx` | 1 | 維持。二つのStubと画面生成だけで前提を読める。 |
| 053 | R・P | `tests/frontends/react/architecture/eslintBoundaries.test.ts` | 3 | 維持。Lintの初期化はbeforeAllへ分離済み。例示するコードは検証に必要な入力。 |
| 054 | R・P | `tests/frontends/react/health/HealthStatus.test.tsx` | 2 | 維持。接続中・正常・失敗を短いStub引数から直接読める。 |
| 055 | R・P | `tests/frontends/react/infrastructure/healthService.test.ts` | 4 | 維持。応答の生成はresponseへ抽出済み。HTTPと本文の条件を直接指定している。 |
| 056 | R・P | `tests/frontends/react/infrastructure/timeRecordService.test.ts` | 8 | 維持。応答生成は抽出済み。DTOの値・時差・小数秒表記は変換検証の条件なので残す。 |
| 057 | R・P | `tests/frontends/react/presentation/timeRecorderState.test.ts` | 4 | 維持。各初期状態はReducerの入力条件。関数で隠さず直接読める形を維持する。 |
| 058 | R・P | `tests/frontends/react/presentation/useHealthStatus.test.ts` | 4 | 維持。単純なStubとdeferredを使用済み。切替と旧応答の解決順序は検証対象。 |
| 059 | R・P | `tests/frontends/react/presentation/useTimeRecordTimes.test.ts` | 7 | 維持。時計の初期化と解除は共通化済み。境界時刻・時間経過・unmountは検証対象。 |
| 060 | R | `tests/frontends/react/presentation/useTimeRecorder.test.ts` | 7 | 一部候補B3。終了・再開始の前提を抽出可能。遅延応答と再読み込みの順序は本文維持。 |
| 065 | R・P | `tests/frontends/react/timeRecords/TimeRecordTimes.test.tsx` | 1 | 維持。時差・小数秒の元表記とtime要素が主題。二つの日時は本文に必要。 |
| 066 | R・P | `tests/frontends/react/timeRecords/TimeRecorder.test.tsx` | 3 | 候補C1。準備は短いが、開始・終了の接続確認に再開始の別仕様が続く。 |

### Pで追加・変更された版

| 番号 | 版 | ファイル | 定義数 | 確認結果 |
| --- | --- | --- | ---: | --- |
| 068 | P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/ApplicationDependenciesTests.cs` | 1 | 維持。Rとの差は参照する型名。初期化は一行で、依存先の検証が主題。 |
| 069 | P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/EndTimeRecordTests.cs` | 1 | 候補B1。状態の生成と保存をまとめる余地がある。Rの修正・改名との統合を先に確認。 |
| 071 | P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/GetTimeRecordsTests.cs` | 2 | 一部候補B1。保存順と完了状態を示すデータは残し、状態の構築だけ抽出できる。 |
| 072 | P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/StartTimeRecordTests.cs` | 1 | 維持。Rとの差は対象型名。準備は二行で、引数と結果を直接読める。 |
| 073 | P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Infrastructure.Tests/TimeRecords/InMemoryTimeRecordStateTests.cs` | 4 | 維持。Addと取得順の操作が検証対象なので、準備として隠さない。 |
| 074 | P | `tests/backends/node/timeRecords.test.ts` | 2 | 一部候補B2。終了テストの二件の開始と時計切替をまとめる余地がある。 |
| 075 | P | `tests/domains/dotnet/Kizamu.Domain.Tests/ProjectTests.cs` | 1 | 維持。名前を指定して生成する一行が検証対象。 |
| 076 | P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs` | 5 | 候補A5。キャンセル前の入力済みポップアップを抽出可能。登録・切替の主題は残す。 |
| 077 | P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/Projects/ProjectTabsViewModelTests.cs` | 3 | 維持。各準備は三行程度。登録後の再表示までの順序は仕様を読むために残す。 |
| 078 | P | `tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs` | 12 | 一部候補B4。二対象の終了テストの準備が長い。切替・応答の順序は本文維持。 |
| 080 | P | `tests/frontends/react/presentation/useTimeRecorder.test.ts` | 5 | 一部候補B3。終了・再開始の前提を抽出可能。Rの追加テストと重複編集しない。 |

### 補助コード

| 番号 | 版 | ファイル | 定義数 | 確認結果 |
| --- | --- | --- | ---: | --- |
| 004 | M | `src/Kizamu.Client/src/test/createTimeRecordApiMock.ts` | — | 維持。既定のAPI応答と必要な上書きを一か所で用意する。 |
| 005 | M | `src/Kizamu.Client/src/test/setup.ts` | — | 維持。画面の後始末を共通化している。 |
| 011 | M | `tests/Kizamu.Application.Tests/TimeRecords/FakeTimeRecordState.cs` | — | 維持。生成ごとに独立する最小の状態Fake。 |
| 017 | R・P | `tests/TestSupport/TestCancellation.cs` | — | 維持。テスト実行コンテキストへの依存を短いプロパティへ集約している。 |
| 023 | R | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/FakeTimeRecordState.cs` | — | 維持。生成・追加・コピーの処理をFakeへ集約済み。B1では呼び出し側の準備を整理する。 |
| 050 | R | `tests/frontends/blazor/TestSupport/StubTimeRecordService.cs` | — | 維持。開始・終了・一覧の固定応答と監視を既存Stubへ集約している。 |
| 051 | R・P | `tests/frontends/blazor/TestSupport/TestHttpMessageHandler.cs` | — | 維持。要求の記録と応答の差し替えを一か所へ集約している。 |
| 061 | R・P | `tests/frontends/react/test/createTimeRecordServiceStub.ts` | — | 維持。既定応答を集約し、個別の条件をoverridesで示せる。 |
| 062 | R・P | `tests/frontends/react/test/deferred.ts` | — | 維持。Promiseの生成手順を隠し、応答を確定する時点を呼び出し側に残している。 |
| 063 | R・P | `tests/frontends/react/test/setup.ts` | — | 維持。Assertの初期化と画面の後始末を集約している。 |
| 064 | R・P | `tests/frontends/react/test/timeRecords.ts` | — | 維持。Domainと表示用日時の構築をまとめ、必要な入力を引数で示せる。 |
| 067 | R・P | `tests/should.mts` | — | 維持。shouldの有効化と型宣言をテスト用の一か所に集約している。 |
| 070 | P | `tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/FakeTimeRecordState.cs` | — | 維持。独立する最小の状態Fake。RのFind・CopyAll追加とは別版として確認した。 |
| 079 | P | `tests/frontends/blazor/TestSupport/StubTimeRecordService.cs` | — | 維持。開始応答を保留する差し替え口により、非同期の解決時点を本文で指定できる。 |

## 初回精査の確認結果と制約（2026-11-10）

* 初回の方針明文化と収集版の全件精査は完了し、この時点では候補のテスト変更は未実施だった。その後の適用状況と次の作業は [現在の次作業](mvp-implementation-plan.md#現在の次作業) を参照する。
* 収集時点ではA5・B4がプロジェクト機能、B3がレビュー対応の作業と重なっていたため、修正時の基準版と適用範囲の確認を必要としていた。
* 他作業版のGit状態確認は、ディレクトリ移動が実行環境から拒否されたため実行できなかった。共有Gitオブジェクトから取得したブランチ先端と、読み取り可能なテストファイルの内容を比較して対象差分を特定した。精査後に全ファイルの内容一致を確認し、一覧外のテスト定義がないことも確認した。
* コード変更がないため、ビルド・テスト・Lintは実行していない。文書の差分検査、相対参照、UTF-8・CRLF、末尾空白、全80行の確認一覧と収集内容の対応、記載した行番号の範囲の検査は成功した。
