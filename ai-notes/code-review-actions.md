# 全コードレビューの修正指示

対象は `codex/readability-second-pass` の `9b0a5fd`。直前のテスト命名規約を含むこの版で、製品・テスト・テスト補助・自作スクリプトをファイルの先頭から末尾まで読んだ。行番号はこのコミットのもの。指摘は精査時点のもの。

**最初に01と02を別々に直す。** どちらもプロジェクトを取り違えるが、01は終了応答、02は一覧応答が引き金になる。片方を直しても他方は残る。03は未指定タブへ戻るための画面操作であり、01・02の修正や再現に先行する必要はない。

| 範囲 | ファイル | 行 | 主な対象 |
| --- | ---: | ---: | --- |
| 製品 | 102 | 3,457 | React、Blazor、Node、ASP.NET Core、両言語のDomain、配信・設定 |
| テスト | 88 | 7,061 | テスト本体、代役・共通準備、依存境界・プロジェクト設定 |
| 自作スクリプト | 3 | 109 | 起動、ヘルスチェック、終了処理 |
| 合計 | 193 | 10,627 | `git ls-files -- src tests scripts` の全件 |

件数と行数は読了範囲を確かめるために使った。既存の計画、判断記録、仕様、ルートのビルド入口も照合した。依存パッケージ、生成物、lockfileの自動生成内容は通読対象外。

## 着手順と検証方法

1. 01–08は製品側の問題。01–03を優先し、その他は利用経路ごとに独立して扱う。新たに保証する振る舞いは、項目に示す失敗を再現してから最小限の実装に進む。04・05は既知の未修正事項を、この版の実行手順に具体化したもの。
2. 09–20は既存テストが見逃す誤動作。先に検証対象を正しく観測できるテストへ直す。正しい実装で最初から成功する場合は、そのことを記録する。
3. 21–32はテストの境界・共通準備・製品の読みやすさ・開発時の確認手順。27は04の動作を確かめた後に着手する。各変更で残す期待値を先に確認し、ひとつの目的ずつ進める。
4. .NET側は該当テスト、関係するソリューションのビルドとformat検査。TypeScript側は該当テスト、ルートの `npm run lint`・`npm test`・`npm run build`。DOMは表示と接続を確かめる箇所で使い、記録の状態遷移と通信の順序はViewModel・Reducer・hookで検証する。

方針の正本は [テストの記述方針](../docs/testing-style.md)、[Kizamuの画面とテストの責務](../docs/ai-instructions/kizamu.md)、[開発上の判断](../docs/decisions.md)。

## 製品側で直す箇所

### 01　終了応答を、終了を要求したプロジェクトに反映する

AとBを記録中にしてAの終了応答を保留し、Bへ切り替えてからAの応答を返す。現状はBの終了対象が消え、Aへ戻ると終了済みのIDが復活する。Reactでは実際のReducerでも確認した。

- **修正**　Blazorは開始処理と同様に要求時のプロジェクトを保持し、応答時はそのプロジェクトに `SetCurrentId` を適用する。Reactは `endTimeRecord` から `recordEnded` に要求元を渡し、表示中のプロジェクトと異なる場合は要求元の保存済みIDだけを解除する。共有一覧のAを終了済みに更新する。
- **完了条件**　ViewModel／hookに遅延応答を渡す。応答直後にBで終了を要求すれば引数はBのID、Aへ戻ったときは終了を要求できない。Aの一覧行の開始・終了はどちらもAの応答値になる。同じタブで終了した既存ケースも通す。 [TimeRecorderViewModel.cs 164行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs#L164) · [useTimeRecorder.ts 87行](../src/frontends/react/src/presentation/useTimeRecorder.ts#L87) · [timeRecorderState.ts 60行](../src/frontends/react/src/presentation/timeRecorderState.ts#L60) · [TimeRecorderViewModelTests.cs 201行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L201) · [useTimeRecorder.test.ts 30行](../tests/frontends/react/presentation/useTimeRecorder.test.ts#L30)

2026-11-20の作業状況。基準コミットは `7005c13`。BlazorとReactで再現テストが期待どおり失敗することを確認後、終了要求時のプロジェクトに応答を反映するよう変更した。変更後は両方の再現テストと既存テストを含む.NET 326件・TypeScript 239件が成功し、両方のビルドと静的検査も成功した。Greenは `b2e7957` に記録済み。テスト配置の整理ではBlazor、ASP.NET Core、Nodeのテスト7ファイルを対応する階層へ移動し、全565テストと両技術のビルド・静的検査が成功した。次は項目02に着手する。

### 02　初回一覧が遅れても、Aで開始した記録をBの終了対象にしない

初回GETを保留し、Aで開始に成功して未開始のBへ移った後、GETに空配列を返す。両実装が合成済み一覧の先頭にあるAをBの終了対象にする。Blazorの公開ViewModelでは、続くBの終了要求がAのIDを送ることも確認した。

- **修正**　一覧へ確定済みの操作を合成する処理と、選択中の終了対象を復元する処理を分ける。終了対象候補から、クライアントが他プロジェクトの進行中IDとして保持するものを除く。未指定も一つのプロジェクトとして扱う。所属情報のないサーバー記録に対する既存の先頭未終了フォールバックは維持する。
- **完了条件**　両実装で上記の順序を再現し、Bは開始可能、Bから終了要求は0回、Aへ戻るとAを終了可能、共有一覧にはAが一件と確認する。既存の復旧テストにある「操作中の対象を優先」「該当がなければ先頭の未終了」も実行する。 [TimeRecorderViewModel.cs 84行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs#L84) · [timeRecorderState.ts 39行](../src/frontends/react/src/presentation/timeRecorderState.ts#L39) · [pendingTimeRecordLoads.ts 1行](../src/frontends/react/src/presentation/pendingTimeRecordLoads.ts#L1) · [TimeRecorderViewModelTests.cs 813行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L813) · [timeRecorderState.test.ts 206行](../tests/frontends/react/presentation/timeRecorderState.test.ts#L206)

修正02の基準は `b4f7911`。BlazorとReactで再現テストが期待どおり失敗することを確認し、別プロジェクトに割当済みの記録を終了対象の候補から外した。.NET 327件・TypeScript 240件の全テストと、両方のビルド・静的検査が成功した。Greenは `3ba1bae` に記録済み。Refactorを確認し、追加変更なし。

### 03　「未指定」を選び直せるタブにする

両画面とも「未指定」は `span` で、登録プロジェクトを選ぶと画面から未指定へ戻れない。未指定で開始した記録があっても、タブを選んで終了できない。内側にはプロジェクトごとの終了対象を保持するテストがある。

- **修正**　両画面の未指定を他のタブと同じく選べる `button`（`role="tab"`）にする。BlazorのタブViewModelは `SelectProject(Project?)` で `null` を受け、Reactは既存の `projectSelected` に `undefined` を渡す。
- **完了条件**　画面ではA登録後に未指定が選択済みでAは未選択と確かめ、未指定→A→未指定の順に選び、各時点の両タブの選択表示と記録欄の見出しを確認する。BlazorのタブViewModelではA選択後に未指定へ戻せることを直接確認する。プロジェクト別の記録状態の全組合せは既存の内側のテストで確認する。 [App.razor 34行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/App.razor#L34) · [ProjectTabsViewModel.cs 17行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/Projects/ProjectTabsViewModel.cs#L17) · [App.tsx 55行](../src/frontends/react/src/App.tsx#L55) · [projectTabsState.ts 35行](../src/frontends/react/src/presentation/projectTabsState.ts#L35) · [AppTests.cs 199行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L199) · [App.test.tsx 122行](../tests/frontends/react/App.test.tsx#L122)

修正03の基準は `3ba1bae`。BlazorとReactの画面テストで、Aを選んだ後に「未指定」のタブが見つからない失敗を再現した。「未指定」を選べるボタンに変更し、選択状態と記録の見出しが切り替わることを確認した。.NET 328件・TypeScript 241件の全テストと両方のビルド・静的検査が成功した。Greenは `8e22096` に記録済み。Refactorを確認し、追加変更なし。

### 04　認証状態を初期注入した場合にもログアウトを反映する

Blazorの表示条件は `IsAuthenticated || timeRecorder is not null`、Reactはサービスを消すと初期propsへフォールバックする。初期認証済みの入口ではログアウト後も保護された画面が残る。実Googleログインの通常起動とは入口が異なる。

- **修正**　注入値は認証状態の初期化時に一度だけ読む。以後の表示、ログイン成功、ログアウトが同一の状態を参照・更新するようにする。Blazorの注入済み記録ViewModelの初回表示も維持する。既知の作業記録の重複起票は不要。
- **完了条件**　初期認証済みで開き、ログアウトすると記録一覧が消え、ログイン入口が表示される。通常の認証経路でもログアウト・再ログインでき、旧プロジェクトを引き継がないことを既存テストと併せて確認する。 [App.razor 68行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/App.razor#L68) · [App.tsx 21行](../src/frontends/react/src/App.tsx#L21) · [AppTests.cs 83行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L83) · [App.test.tsx 79行](../tests/frontends/react/App.test.tsx#L79)

修正04の基準は `8e22096`。BlazorとReactの画面テストで、初期認証済みで開くとログアウト後も一覧が残る問題を再現した。ログアウトで初期認証状態を解除するよう変更し、一覧が消えてGoogleログインボタンが戻ることを確認した。.NET 329件・TypeScript 242件の全テストと両方のビルド・静的検査が成功した。Greenは `0b30b39` に記録済み。Refactorを確認し、追加変更なし。

### 05　Googleログインボタンの再描画でBlazorのコールバック参照を蓄積しない

`RenderButtonAsync` は毎回 `DotNetObjectReference` を作り、`Dispose` が解放できるのは最後の一つ。ログアウトでボタンが再表示されるたびに以前の参照が残る。現在の画面テストは `IGoogleSignIn` の代役を使う。

- **修正**　同じ `GoogleSignIn` の存続中はJSへ渡す参照を再利用し、受け取った通知先だけを差し替える。破棄時に参照を解放し、通知先もクリアする。
- **完了条件**　実際の `GoogleSignIn` に記録用のJSランタイムを渡し、二度のボタン表示で同一の参照を受け取ること、二回目以降の通知は最新のコールバックに一度だけ届くこと、破棄後に古い通知先を呼ばないことを確認する。 [GoogleSignIn.cs 13行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/GoogleSignIn.cs#L13) · [App.razor 71行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/App.razor#L71) · [GoogleSignInTests.cs 14行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/GoogleSignInTests.cs#L14)

修正05の基準は `0b30b39`。GoogleSignInの再描画で参照が増えることと、破棄後に旧通知先が呼ばれることを再現した。同一インスタンスで参照を再利用し、破棄時に通知先を解除するよう変更した。.NETの全333テスト、ビルド、静的検査が成功した。Greenは `032fac7` に記録済み。Refactorを確認し、追加変更なし。

### 06　壊れた記録応答をDomainへ渡す前に検出する

Reactは `id === undefined` だけを判定するため、JSONの `null` と数値も記録IDになり得る。BlazorのDTOは `Id` だけ必須で、`startedAt` の欠落が `DateTimeOffset` の既定値になる。実在しない開始時刻が画面に届く。

- **修正**　技術ごとに独立して直す。Reactは変換境界でIDが文字列かを判定する。Blazorは `StartedAt` をJSONの必須プロパティとする。終了時刻が省略された場合の扱いはAPIが返す明示的な `null` と区別する（項目10参照）。
- **完了条件**　既存の開始応答のテストに、Reactでは欠落・null・数値ID、Blazorでは開始時刻の欠落を与え、変換失敗を観測する。正常な未終了・終了済み応答の復元は両方で確認する。ID文字列の形式に新条件を設ける必要はない。 [timeRecordService.ts 45行](../src/frontends/react/src/infrastructure/timeRecordService.ts#L45) · [TimeRecordResponse.cs 10行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure/TimeRecords/TimeRecordResponse.cs#L10) · [timeRecordService.test.ts 64行](../tests/frontends/react/infrastructure/timeRecordService.test.ts#L64) · [TimeRecordApiTests.cs 1行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure.Tests/TimeRecords/TimeRecordApiTests.cs#L1)

修正06のReact側の基準は `032fac7`。成功応答のIDがnullまたは数値の場合に記録が復元されることを再現し、通信サービスの変換時にIDの型を検証するよう修正した。TypeScriptの全244テスト、ビルド、静的検査が成功した。Greenは `99a94ee` に記録済み。Refactorを確認し、追加変更なし。

修正06のBlazor側の基準は `550f954`。HTTP 201の応答に開始時刻がないときに例外が発生しないことを再現し、開始時刻を必須にした。.NETの全334テスト、ビルド、静的検査が成功した。Greenは `fdb7fab` に記録済み。Refactorを確認し、追加変更なし。

### 07　両APIでBearer認証方式名の大文字小文字を同じように扱う

Nodeは先頭の `Bearer ` を大小文字まで固定して照合し、ASP.NET Coreは方式名を大小文字を区別せず照合している。同じ有効な認証情報で結果が異なる。

- **修正**　Nodeでは方式名だけを大小文字を区別せず判定し、資格情報そのものは元の文字列のまま既存の検証器に渡す。
- **完了条件**　両APIでテスト用の有効な資格情報を用い、小文字の方式名による一覧取得が200となることを確認する。既存の未指定・別方式の拒否も実行する。 [timeRecordEndpoints.ts 35行](../src/backends/node/api/timeRecordEndpoints.ts#L35) · [TimeRecordAuthentication.cs 10行](../src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api/Endpoints/TimeRecords/TimeRecordAuthentication.cs#L10) · [timeRecordEndpoints.test.ts 40行](../tests/backends/node/api/timeRecordEndpoints.test.ts#L40) · [TimeRecordEndpointTests.cs 47行](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/Endpoints/TimeRecords/TimeRecordEndpointTests.cs#L47)

修正07の基準は `fdb7fab`。小文字の認証方式名を使った一覧取得が401になることをテストで再現し、方式名を大小文字を区別せず判定するよう修正した。TypeScriptの全245テスト、lint、ビルドが成功した。委任ツールを使用できないため、この工程は同じ担当者がRed・Greenを実施した。Greenは `1672e12` に記録済み。Refactorを確認し、追加変更なし。

### 08　Nodeの公開鍵取得に有効期限を適用する

Nodeの検証器は有効な公開鍵を取得した直後でも、次の記録操作で全鍵を再取得・再変換する。鍵配信への往復が毎回必要で、一時的な通信失敗がその間の記録操作にも及ぶ。

- **修正**　既に導入している `google-auth-library` の `getFederatedSignonCertsAsync()` に公開鍵取得と有効期限処理を任せる。テストでは既存の偽通信をライブラリの通信境界へ移す。署名、発行元、利用先、期限、本人識別の判定条件と、鍵取得失敗を上位に伝える動作を保つ。
- **完了条件**　固定時計とキャッシュ期限付きの偽応答で、有効期間内の連続二件は鍵取得一回、期限後の検証は再取得することを確認する。既存の正常・拒否ケースも通し、実Googleへの通信を必要としない。 [googleIdTokenVerifier.ts 18行](../src/backends/node/infrastructure/googleIdTokenVerifier.ts#L18) · [googleIdTokenVerifier.test.ts 5行](../tests/backends/node/infrastructure/googleIdTokenVerifier.test.ts#L5)

修正08の基準は `0478118`。公開鍵の有効期間内でも毎回取得することを再現し、採用済みライブラリの公開鍵取得と有効期限キャッシュを利用するよう変更した。テスト用の鍵応答をPEM形式に合わせた。TypeScriptの全246テスト、lint、ビルドが成功した。Greenは `1eebe8e` に記録済み。Refactorを確認し、追加変更なし。

## テストが見逃している箇所

### 09　未知IDで終了した後、保存先にある記録を読み直す

`timeRecord.EndedAt` は操作前に保持した不変オブジェクトを検査している。別の終了済みインスタンスで保存先を誤更新しても通る。

`Execute` の後で `state.Find(timeRecord.Id).Should().Be(timeRecord)` とする。取得できること、元のID・開始時刻・未設定の終了時刻を同時に確かめられる。精査時点では未知IDに対する戻り値や例外の契約を保留していた。 [TimeRecordEndOperationTests.cs 99行](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordEndOperationTests.cs#L99)

修正09の基準は `1eebe8e`。未知IDでの終了後に保存先から元の記録を取得し、値全体が元のままか確認するようテストを変更した。.NETの全334テスト、ビルド、静的検査が成功した。未知IDの戻り値と例外は当時未確定だった。次は修正10へ進む。

2026-11-20に `d11b5d8` を基準として、[D009](../docs/decisions.md#d009終了対象をidで取得し再終了では終了時刻を更新する) の対象不在の契約を実装した。両バックエンドのApplicationとHTTPに追加した4件は実装前に失敗を確認した。実装後はASP.NET Core側84件、npm側224件がすべて成功し、ビルドと静的検査も成功した。次はRefactorの要否を確認する。

### 10　記録開始のHTTP応答で、endedAtのキーとnullを確認する

ASP.NET側のテストはデシリアライズしたnullableプロパティを読むため、`endedAt` がJSONから消えても通る。Reactの変換処理はキーの欠落を明示的な `null` と別に扱う。

既存の201テストで応答本文をJSONとして読み、正確なキー名 `endedAt` があり `JsonValueKind.Null` であることを確認する。現在のIDと開始時刻の検証も維持する。Nodeの対応テストは既にキーを確認している。 [TimeRecordEndpointTests.cs 145行](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/Endpoints/TimeRecords/TimeRecordEndpointTests.cs#L145) · [timeRecordEndpoints.test.ts 187行](../tests/backends/node/api/timeRecordEndpoints.test.ts#L187)

修正10の基準は `3adf0ca`。開始応答のJSONにendedAtキーがnullで含まれることを独立したテストで確認した。.NETの全335テスト、ビルド、静的検査が成功した。委任ツールを利用できないため統括が担当した。次は修正11へ進む。

### 11　期限切れの資格情報を記録APIが拒否する接続を確認する

単体での有効期限の検証と、APIでの欠落・別方式の拒否はある。検証器が期限切れを返したときにAPIが操作を拒否する接続がテストされていない。

各APIでテスト用の所有者の記録を一件開始し、同じ利用者の期限切れテスト資格情報で終了を要求する。応答が401で、その後、所有者の有効な資格情報で取得した同じ記録の終了時刻がnullであることを確認する。他の検証条件の組合せは検証器の単体テストに残す。 [TimeRecordEndpointTests.cs 47行](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/Endpoints/TimeRecords/TimeRecordEndpointTests.cs#L47) · [timeRecordEndpoints.test.ts 40行](../tests/backends/node/api/timeRecordEndpoints.test.ts#L40) · [GoogleIdTokenVerifierTests.cs 63行](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Infrastructure.Tests/Authentication/GoogleIdTokenVerifierTests.cs#L63) · [googleIdTokenVerifier.test.ts 1行](../tests/backends/node/infrastructure/googleIdTokenVerifier.test.ts#L1)

修正11の基準は `856cbf8`。両APIで所有者の記録に期限切れ資格情報で終了を要求すると401となり、有効な資格情報で再取得した記録が未終了のままであるテストを追加した。追加テストは既存実装で成功。.NET 336件・TypeScript 247件の全テスト、ビルド、静的検査が成功した。formatの初回実行ではワークスペース読み込み警告が出たが、診断出力付きの再実行では再現せず原因は未特定。次は修正12へ進む。

### 12　開始時の時計を二回の要求で読み直すことを検証する

Nodeの「開始するたびに…現在時刻を開始時刻とする」テストは二回とも時計が同じ。二件目で最初の時刻を再利用しても通る。

`start()` の二回の要求の間で `now` を進め、それぞれの開始時刻と発行元が渡した識別子を確認する。開始直後の終了時刻はDomainの直接テストで保証する。 [timeRecords.test.ts 38行](../tests/backends/node/application/timeRecords.test.ts#L38)

修正12の基準は `21bf4a6`。同じApplicationに二回開始を要求し、時計を進める前後の時刻と発行元の識別子を確認する形に整理した。差分は外部仕様から見たテスト整理に含め、`f33b9df` でコミット済み。

### 13　Nodeの起動テストでURLの行末を待つ

標準出力の一回の `data` がポート番号の途中で切れると、正規表現が途中までを正しいURLとして返す。環境によってヘルスチェックの宛先が変わる。

`waitForListeningUrl` は改行を受け取った完全な行からURLを読む。疑似ストリームに `http://localhost:52`、続いて `13\n` を渡し、最初の入力では未完了、次でポート5213となることを確認する。既存の実プロセス起動、早期終了、タイムアウト、後始末を維持する。 [serverStartup.test.ts 50行](../tests/backends/node/serverStartup.test.ts#L50)

修正13の基準は `737b0fb`。先行ログ行の後に分割されたURLを使うテストで、途中のポート番号で完了する失敗を確認し、URLを含む行の改行まで待つよう修正した。Nodeを含むTypeScriptの全222テスト、lint、ビルドが成功。`d11b5d8` でコミット済み。

### 14　再開始の表示を同じ画面の更新として確認する

Blazorは再開始済みの値だけを与えて初描画し、以前の終了欄が残る不具合を検出できない。Reactは反対に、表示を調べる前提を開始→終了→開始の画面操作で作っている。

Blazorは終了済みの画面を取得し、同じコンポーネントに新しい記録のスタブ値を渡して再描画する。Reactは既存の `TimeRecorderView` を終了済み状態で描画し、新しい `displayedRecord` と一覧を渡して `rerender` する。両方で更新前の終了時刻の存在、更新後の新しい開始時刻と終了欄の消失を操作欄に限定して確認する。状態遷移の全条件はViewModel／Reducer側に残す。 [TimeRecorderTests.cs 153行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecorderTests.cs#L153) · [TimeRecorder.test.tsx 205行](../tests/frontends/react/timeRecords/TimeRecorder.test.tsx#L205) · [timeRecorderState.test.ts 239行](../tests/frontends/react/presentation/timeRecorderState.test.ts#L239)

### 15　「記録欄」「一覧」と書いた画面テストをその領域で検証する

Blazorの「記録欄に終了ボタンだけ」は画面全体から開始・終了の文字列を探す。Reactの作業時間は同じ文字列が画面内に二つあれば通る。同じ欄への二重表示や別の欄への誤配置を見逃す。

Blazorは `.recorder-panel button` のテキスト列を `Equal("記録を終了")` と比較する。Reactは操作欄の名前付きregionと「作業記録一覧」のlistを `within` で分け、各々に `1時間30分` が一つあることを確認する。 [AppTests.cs 236行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L236) · [TimeRecorder.test.tsx 192行](../tests/frontends/react/timeRecords/TimeRecorder.test.tsx#L192)

### 16　手動再取得が利用者操作の中で実行されることを確かめる

`LoadAsync` と `RunUserOperationAsync(It.IsAny<Func<Task>>())` の回数を独立して見るだけでは、空の委譲と別呼び出しの `LoadAsync` でも通る。

モックで渡された `Func<Task>` を捕捉して一旦保留する。再取得クリック時点では初回読込の一回だけ、捕捉した関数を実行すると二回目となることを確認する。開始・終了を呼んでいない検証も残す。 [TimeRecorderTests.cs 209行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecorderTests.cs#L209) · [TimeRecorder.razor 24行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecorder.razor#L24)

修正14〜16は `5ef1933` のテスト整理で対応済み。同一画面の再描画、操作欄と一覧の領域別検証、手動再取得の委譲先の実行を確認している。

### 17　時刻表示テストで分の境界と各時刻の名前を直接観測する

「表示が一分進む」は進んだ後しか見ず、アクセシブルな名前のテストは属性の個数しか見ない。固定表示やラベルの入れ替えでも通る。

同一の描画済みコンポーネントで90分59秒時点の `1時間30分` と、一秒後の `1時間31分` を確認する。名前のTheoryでは二つの `time` の `aria-label` を表示順に比較し、操作欄は「開始時刻」「終了時刻」、一覧は両方とも属性なしと確認する。 [TimeRecordTimesTests.cs 64行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecordTimesTests.cs#L64) · [TimeRecordTimesTests.cs 116行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecordTimesTests.cs#L116)

修正17の基準は `d11b5d8`。分境界の前後を同じコンポーネントで確認し、時刻のアクセシブルな名前を表示順に比較する。対象10件を確認済み。コミット前の再検証では.NET全302テスト、非増分ビルド、変更ファイルのformatが成功。全体のformat検査は既知のBL0016警告のみで終了コード2。

### 18　開始通信中の終了抑止を、有効な終了対象で試す

今のhookテストは初期一覧が空のまま終了を試す。開始中の排他判定を外しても、終了対象IDなしの早期returnで通信0回になる。

同じイベント内での二重開始のテストは保持する。追加終了のケースではAの開始を完了、Bで開始応答を保留、Aへ戻ってAのIDがあることを確かめてから終了を試す。終了APIは0回、Bの開始応答を解決した後にはAを終了できることを確認する。 [useTimeRecorder.test.ts 30行](../tests/frontends/react/presentation/useTimeRecorder.test.ts#L30) · [useTimeRecorder.ts 87行](../src/frontends/react/src/presentation/useTimeRecorder.ts#L87)

### 19　依存境界の許可例が構文エラーでも成功する穴を閉じる

許可例が禁止ルールIDだけを絞り込むため、構文エラーなど別の診断で参照解析自体が失敗しても成功する。

許可例はESLintの生の `report.messages` が空であることを比較する。Domainの補助がルール名だけを返す場合は許可側で生の診断を受ける。確認した許可例6件は全て診断0件。禁止例は無関係な診断が併発し得るので期待するルールIDを引き続き個別に確認する。 [dependencyBoundaries.test.ts 54行](../tests/domains/typescript/dependencyBoundaries.test.ts#L54) · [eslintBoundaries.test.ts 46行](../tests/frontends/react/architecture/eslintBoundaries.test.ts#L46)

### 20　ログインボタンとログアウトボタンの位置を同じ基準で比較する

Reactのテストは `parentElement` の一致だけで、同じ `main` の末尾に移動しても成功する。Blazor側に表示位置を比較する例がある。

両ボタンをそれぞれ含む `main` 直下の子要素の位置を、ログイン前後で比較する。ログイン前の位置が見つかったことも確認する。ボタンにラッパーがあっても同じ位置なら通り、フッターの前まで移すと失敗する。 [App.test.tsx 37行](../tests/frontends/react/App.test.tsx#L37) · [AppTests.cs 72行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L72)

## 読みやすさ・責務・開発用スクリプト

### 21　一覧の到着が主題のテストで、応答を返す行を本文に置く

古い一覧が届くこと自体を検証するテストで、応答の内容も到着時点も準備メソッドに隠れている。

748・756・796・804行付近のケースでは、共通準備は応答が保留された時点で終え、応答と読込Taskをタプルで返す。本文に `SetResult(古い一覧)` → `await 読み込み` → 期待結果を並べる。受信後さらに別操作を検証するケースの準備は共有できる。467行の状態確認は古い応答の直後に行い、追加の開始・終了の禁止は直後の専用テストに残す。 [TimeRecorderViewModelTests.cs 467行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L467) · [TimeRecorderViewModelTests.cs 748行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L748) · [TimeRecorderViewModelTests.cs 796行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L796) · [TimeRecorderViewModelTests.cs 1059行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L1059)

### 22　Domainの生成・終了の仕様をDomainのテストで読めるようにする

TSの開始・復元を検証するケースは操作自体が `beforeEach` にあり、通信テストでは逆に一覧を取得した後 `TimeRecord.end()` の全条件を繰り返している。

開始・復元そのものを検証する二件は本文から `TimeRecord.start` とコンストラクタを呼ぶ。開始済み・復元済みが前提の他のケースは共通準備のままとする。通信側は一覧の `toStrictEqual` でクラス、順序、各値を保証していること、Domain側は終了時刻・ID・新インスタンス・元の不変性を保証することを確認し、通信経由の終了テストだけを取り除く。 [timeRecord.test.ts 24行](../tests/domains/typescript/timeRecord.test.ts#L24) · [timeRecord.test.ts 53行](../tests/domains/typescript/timeRecord.test.ts#L53) · [timeRecordService.test.ts 33行](../tests/frontends/react/infrastructure/timeRecordService.test.ts#L33) · [timeRecordService.test.ts 104行](../tests/frontends/react/infrastructure/timeRecordService.test.ts#L104)

### 23　再取得の成否を検証する画面テストから重複した状態遷移を外す

開始失敗と一覧失敗を画面から重ねて、復旧後の可否と再失敗を繰り返している。hookと操作状態Reducerに同じ遷移の直接テストがある。

画面には再取得クリックが一覧取得につながって通知が変わる代表例を一件残す。239・245行付近の各期待値を、hookの226・234・243行付近（一覧・終了要求・再失敗）とViewへの直接入力（警告・操作ボタン・再取得可能）に対応づけてから重複経路を除く。画面から外す期待値のうち、手動再取得の応答が空なら次の開始要求が一回届くことを、該当するhookテストに明記する。再失敗時に初回を含め一覧要求が計三回であることは現行hookの251行で確認済み。保留中のボタン無効化はViewのテストに残す。 [TimeRecorder.test.tsx 217行](../tests/frontends/react/timeRecords/TimeRecorder.test.tsx#L217) · [timeRecorderOperationState.test.ts 54行](../tests/frontends/react/presentation/timeRecorderOperationState.test.ts#L54) · [useTimeRecorder.test.ts 226行](../tests/frontends/react/presentation/useTimeRecorder.test.ts#L226)

### 24　認証ヘッダーのテストで、操作の選択と通信の共通準備を分ける

Reactは日本語の操作名を複数の `if` で解釈する。Blazorは一つのTheory本文にハンドラー、応答JSON、クライアント寿命、操作の選択が並ぶ。

Reactは各ケースに表示名・操作関数・応答・期待URL・メソッドを持たせ、本文は操作を一度呼ぶ。呼出し一回・URL・メソッド・ヘッダーを維持する。Blazorはハンドラー・クライアントをテスト単位で初期化・破棄し、一覧／単一記録の応答準備を補助へ移す。三つの操作と期待 `Bearer id-token` が読み取れる形にする。 [timeRecordAuthorization.test.ts 16行](../tests/frontends/react/infrastructure/timeRecordAuthorization.test.ts#L16) · [TimeRecordAuthorizationTests.cs 15行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure.Tests/TimeRecords/TimeRecordAuthorizationTests.cs#L15)

### 25　通常起動テストでGoogleボタンの代役を準備メソッドに移す

Google SDKのコールバック保存とDOMボタン生成が、通常起動→ログイン→認証付き一覧という主題を埋めている。健康確認の代役も実際の正常値 `ok` と異なる。

SDKの代役を同ファイルの名前付き準備へ移し、ヘルス応答は `{ status: 'ok' }` にする。本文には本物の起動入口、ログイン、期待する一覧要求を残す。`some(...).should.equal(true)` は観測したURLとAuthorization値の組に期待値が含まれることを `waitFor` で比較し、失敗時に実際の要求を読めるようにする。 [startup.test.tsx 10行](../tests/frontends/react/startup.test.tsx#L10) · [healthService.ts 12行](../src/frontends/react/src/infrastructure/healthService.ts#L12)

### 26　各言語内でテスト用の署名鍵と検証用公開鍵の準備を共有する

APIテストと検証器テストが、同じ技術内でテスト資格情報の生成と偽の鍵配信を別々に実装している。ASP.NETのAPIテストの静的RSA鍵には破棄する持ち主がいない。

C#はテスト側の共有ソースと寿命を持つfixture、Nodeはバックエンドのテスト用モジュールへ、同じ鍵から資格情報と公開鍵応答を作る共通部分を移す。所有者・期限・発行元・異なる署名鍵など、各テストが変える値は本文に残し、既存の鍵IDと時刻条件を保つ。各言語で正常・期限切れ・不正署名、利用者間の分離テストを通す。 [TimeRecordEndpointTests.cs 241行](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/Endpoints/TimeRecords/TimeRecordEndpointTests.cs#L241) · [GoogleIdTokenVerifierTests.cs 80行](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Infrastructure.Tests/Authentication/GoogleIdTokenVerifierTests.cs#L80) · [timeRecordEndpoints.test.ts 15行](../tests/backends/node/api/timeRecordEndpoints.test.ts#L15) · [googleIdTokenVerifier.test.ts 41行](../tests/backends/node/infrastructure/googleIdTokenVerifier.test.ts#L41)

### 27　通常起動のAppからプロジェクト画面の責務を分ける

Reactの `App` に認証、Googleボタンの描画、タブ、登録ダイアログが集中している。BlazorはSDK通知と代替ログインで、トークンから記録サービスとViewModelを作る部分が重なる。

項目04を先に確認する。Reactは認証状態とログイン入口をAppに残し、認証済み画面と既存のタブReducerをまとめて抽出する。登録直前の入力・キャンセル・選択を既存テストで維持する。Blazorは二つの経路で共有する「認証結果から記録用ViewModelを作る」部分を一か所にする。SDK通知で必要な再描画と、代替ボタンのイベント完了に伴う再描画の違いを保つ。 [App.tsx 18行](../src/frontends/react/src/App.tsx#L18) · [App.razor 81行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/App.razor#L81) · [projectTabsState.ts 24行](../src/frontends/react/src/presentation/projectTabsState.ts#L24) · [App.test.tsx 95行](../tests/frontends/react/App.test.tsx#L95)

### 28　ISOの元表記、日時点、HTMLの文字列をテストとコメントで区別する

Presentationのコメントは「HTML timeに元のISO表記を使う」とするが、ビューはDomainの日時点からHTML向けの表記を生成する。画面テストでは時刻取得と形式の正規表現が二重になっている。

コメントを通信で受けた元表記を保持する説明に直す。画面テストは開始・終了をラベルと入力の表にし、各要素の `datetime` の文法と表す日時点を一回ずつ検査する。UTCミリ秒への切り捨ては既存の文字列期待値のまま残す。通信テストは元の文字列と `Temporal.Instant` の比較、更新テストは元表記と参照同一性を引き続き区別する。 [timeRecordPresentation.ts 3行](../src/frontends/react/src/presentation/timeRecordPresentation.ts#L3) · [TimeRecordTimes.tsx 20行](../src/frontends/react/src/timeRecords/TimeRecordTimes.tsx#L20) · [TimeRecordTimes.test.tsx 16行](../tests/frontends/react/timeRecords/TimeRecordTimes.test.tsx#L16) · [timeRecordService.test.ts 124行](../tests/frontends/react/infrastructure/timeRecordService.test.ts#L124) · [timeRecorderState.test.ts 195行](../tests/frontends/react/presentation/timeRecorderState.test.ts#L195)

### 29　ダイアログの配置に効かないCSSを外す

両画面のダイアログでボタンの親はflexだが、子に `justify-self` が指定されている。Reactの狭幅用 `.records-list li` は通常幅と同じ列指定を重ねている。

ダイアログの二つのボタンから通常幅と狭幅の `justify-self` だけを外し、Reactでは重複する一覧行の列指定も外す。通常幅と38rem以下で一覧の列、ボタンの並び・折り返し・フォーカス表示を変更前後で照合する。 [index.css 240行](../src/frontends/react/src/index.css#L240) · [index.css 382行](../src/frontends/react/src/index.css#L382) · [app.css 240行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Client/wwwroot/app.css#L240)

### 30　起動確認が200以外の成功応答で再試行を使い切らないようにする

共有の起動待機はHTTP例外の場合だけ待つ。起動中に204などの応答を受けると、間を空けずに60回問い合わせて正常起動前に失敗と判定し得る。

HTTP 200なら即成功、プロセス終了なら即失敗とし、そのどちらでもなかった試行の後に既存の待機を一度だけ行う。最後の試行後の余分な待ちは省ける。代役で204→200、例外→200、プロセス終了、規定回数まで200なしを確認する。 [Wait-HealthEndpoint.ps1 7行](../scripts/Wait-HealthEndpoint.ps1#L7) · [Check-React.ps1 34行](../scripts/Check-React.ps1#L34) · [Check-Blazor.ps1 1行](../scripts/Check-Blazor.ps1#L1)

### 31　起動失敗時に、Reactの確認スクリプトから原因をたどれるようにする

子プロセスの出力を一時ファイルに保存しているが、失敗時に一般的なエラーを出した直後、finallyでログディレクトリを消す。失敗したプロセスとその診断が分からない。

失敗したプロセス名と終了コード（取得可能な場合）、該当プロセスの起動エラーの要点を、後始末の前に表示する。トークンや環境変数の一括出力は不要。直ちにエラー終了する代役で原因が読め、他に起動したプロセスと一時ログは従来どおり片付くことを確認する。 [Check-React.ps1 34行](../scripts/Check-React.ps1#L34) · [Check-Blazor.ps1 1行](../scripts/Check-Blazor.ps1#L1)

### 32　テスト名に、実際の条件・主体・観測結果を書く

下表の名前はそのまま変更案として使える。同じ名前を機械的に展開せず、記録の要求を送ったのか、保存結果を再読したのか、画面に表示したのかを、各テストの期待値と一致させる。

| 現在の箇所 | 変更後のテスト名 | 条件・確認するもの |
| --- | --- | --- |
| 接続確認の応答待ち<br>[HealthStatusViewModelTests.cs 27行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/Health/HealthStatusViewModelTests.cs#L27) | サーバー接続を初めて確認すると応答が届くまで接続状態は未確定です | 未確定なのは初回。二回目以降は前回の結果を持ち得る。 |
| Googleログインで得たトークンの引き渡し<br>[AppTests.cs 61行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L61) | ログインボタンで取得したIDトークンを記録サービスの生成処理に渡します | モックが観測するのはサービスを作る引数。 |
| Googleから受け取ったトークンの引き渡し<br>[GoogleSignInTests.cs 45行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/GoogleSignInTests.cs#L45) | Googleの認証コールバックで受け取ったIDトークンを記録サービスの生成処理に渡します | SDKのコールバックと代替ボタンの経路を区別する。 |
| ログイン直後の開始ボタン<br>[AppTests.cs 129行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L129) | 記録がない利用者がログイン済みの画面を開くと記録欄に開始ボタンを表示します | 初期認証済みとして描画する。ログイン操作はこのテストでは行わない。 |
| 再ログイン後の記録中表示・一覧<br>[App.test.tsx 60行](../tests/frontends/react/App.test.tsx#L60) · [AppTests.cs 103行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L103) · [AppTests.cs 113行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L113) | 再ログイン後の一覧が空なら、記録欄は待機中になり開始ボタンを表示します ／ 再ログイン後の一覧が空なら、前のログイン時の記録行は表示されません | 二回目のサービスの空の一覧を準備で明示し、それぞれが観測する表示を選ぶ。サーバーが前回の記録を返す場合まで消去すると読める名前を避ける。 |
| 手動再取得後に記録を終了・開始できる<br>[TimeRecorderViewModelTests.cs 554行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L554) · [TimeRecorderViewModelTests.cs 565行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L565) | 手動で再取得した一覧に未終了の記録があるとそのIDで終了を要求できます ／ 手動で再取得した一覧の記録が終了済みなら開始を再び要求できます | 観測しているのはEndedIds／StartCount。サーバーでの完了までをテスト名に含めない。 |
| 新しく開始した記録から終了する<br>[TimeRecorderViewModelTests.cs 831行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L831) | 一覧応答の先頭に別の未終了記録があっても一覧取得中に開始した記録のIDで終了を要求します | 終了要求は一回。複数件の終了順を検証する名前を避ける。 |
| 未認証で開始した後の一覧<br>[TimeRecordEndpointTests.cs 74行](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/Endpoints/TimeRecords/TimeRecordEndpointTests.cs#L74) · [timeRecordEndpoints.test.ts 62行](../tests/backends/node/api/timeRecordEndpoints.test.ts#L62) | 未認証で記録の開始を要求した後も認証済み利用者の一覧は空です | 開始を要求した主体と、取得した一覧の所有者が異なる。読取時のテスト用利用者を明示する。 |
| 未認証で終了した後の一覧<br>[TimeRecordEndpointTests.cs 95行](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/Endpoints/TimeRecords/TimeRecordEndpointTests.cs#L95) · [timeRecordEndpoints.test.ts 83行](../tests/backends/node/api/timeRecordEndpoints.test.ts#L83) | 未認証で終了を要求しても所有者の一覧では記録の終了時刻は未設定のままです | Nodeのケースが記録全体を比較する場合は「所有者の一覧には開始時の記録がそのまま残ります」とし、既存の全体比較を維持する。 |

## 外部仕様から見たテストの重複と結合

2026-11-20に `21bf4a6` と作業中の修正12を対象に、両バックエンド、両フロントエンド、C#とTypeScriptのDomainのテストを照合した。項目ごとに観測した重複と、反映後の保証を記録した。リンクは反映後のテストを指す。

業務規則はDomainの公開操作で、対象選択・時計・保存・結果の変換はApplicationの公開操作で検証する。画面状態と操作の調整はViewModel／Reducer／hookで観測する。HTTPと画面では、経路の接続、利用者に返す応答、表示・入力など、その境界で初めて起こり得る誤りを検証する。

### 終了・再終了で選んだ記録が返り、保存される

* C#のApplicationでは、単一レコードの終了結果・保存結果と、二件から選ぶ場合の同じ結果が重なる。開始時刻の異なる二件を保存し、先に保存した方をIDで選ぶ。返却はID・開始時刻・要求時刻を照合し、保存は選んだ記録の更新と対象外の記録の保持を照合する。返却と保存は別々のテストにし、記録中と終了済みの両方、異なる要求時刻をケースに残す。未知IDについては既存の記録を保持するテストを残す。[TimeRecordEndOperationTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordEndOperationTests.cs#L28)
* C#のDomainに、終了済み記録の `End` が新しい終了時刻を持つ記録を返し、元の終了時刻を保つケースを置く。TypeScriptには両方のテストがある。NodeのApplicationの初回終了・再終了も、事前に二件を保存し、返却と保存をそれぞれ検証する形にそろえる。期待する更新には、対象レコードにテストで指定した要求時刻を渡したDomainの結果を使える。実際に読み出し・差し替えを行う経路は両状態で保持する。[C# Domain](../tests/domains/dotnet/Kizamu.Domain.Tests/TimeRecordTests.cs#L114) · [TypeScript Domain](../tests/domains/typescript/timeRecord.test.ts#L59) · [Node Application](../tests/backends/node/application/timeRecords.test.ts#L71)

### 開始のたびに時計を読み、開始した記録を保存する

* C#の空でないIDの生成と、両言語の開始直後の終了時刻未設定はDomainに直接のテストがある。Applicationでは時計を進めながら同じ対象に二回要求し、それぞれ時計の値を使うことを確認する。C#の開始結果のID・時刻が保存した記録と対応し、保存時に未終了状態が反映される接続は代表例で確認する。Nodeでは発行元が返した異なるIDを使う確認も残す。項目12の全フィールド比較を時計とIDの保証へ絞り、一覧から開始した記録を取得できるテストを残した。[C#開始操作](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordStartOperationTests.cs#L19) · [C# Domain](../tests/domains/dotnet/Kizamu.Domain.Tests/TimeRecordTests.cs#L64) · [Node開始操作](../tests/backends/node/application/timeRecords.test.ts#L38)

* NodeのHTTPでも二回の開始要求を送るテストが二件ある。異なる二件を開始順に返す一覧テストで、二つの開始応答のIDが異なることも確認できる。発行元の単体テストと別に、実際のHTTP構成で二回の開始に別々のIDを渡す代表経路を維持する。[Node API](../tests/backends/node/api/timeRecordEndpoints.test.ts#L114)

### 拒否した要求の応答と保存結果を同じ要求で観測する

* 両APIに、同じ初期状態・同じPOSTを、ステータスと状態の確認でそれぞれ送るケースがある。開始と終了は独立したルートとして各テストを残し、各ケースでPOSTを一回送り、ステータスと所有者の再取得結果を確認する。403は既存の `Sec-Fetch-Site` 5条件をすべて残す。認証情報なしの401、他の利用者による終了の404も同様に、開始なら空の一覧、終了なら保存した記録を読み直し、現行の未終了または変更前との一致を照合する。項目32の未認証終了の命名も同じ修正に含める。[ASP.NET Core](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/Endpoints/TimeRecords/TimeRecordEndpointTests.cs#L185) · [Node](../tests/backends/node/api/timeRecordEndpoints.test.ts#L135)

### HTTPで値の形式と日時点を重ねて比較する箇所を整理する

* Nodeの開始・終了応答は、同じプロパティを固定のISO文字列と比較し、`Date` に変換した日時点とも比較する。ISO文字列の完全一致で両方の意図を満たすので、文字列の期待値を残す。C#の項目10が保証するJSONの `endedAt` キーの存在とnullは、DTOの値だけを検証するテストでは観測できないため、キーを直接確認するテストを使う。HTTPの値そのものを確認する代表経路は残す。[Node終了](../tests/backends/node/api/timeRecordEndpoints.test.ts#L104) · [Node開始](../tests/backends/node/api/timeRecordEndpoints.test.ts#L173) · [ASP.NET Core JSON](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/Endpoints/TimeRecords/TimeRecordEndpointTests.cs#L140)

### 保存一覧のコピーが最初の記録を保持する

* C#のInfrastructureで、一覧をコピーして次の記録を追加する準備が二件で共通している。コピーに一件だけ残ることと、その一件が最初の記録と同じインスタンスであることを、同じコピーの観測で表す。元の一覧への追加の反映、およびApplicationの列挙開始時の取得と列挙中の固定は、それぞれ異なる呼び出しの保証なので維持する。[InMemoryTimeRecordStateTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Infrastructure.Tests/TimeRecords/InMemoryTimeRecordStateTests.cs#L68) · [TimeRecordReaderTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordReaderTests.cs#L40)

### Nodeの起動確認では出力されたURLへ接続する

* `/api/health` のJSON本文は、Content-Typeも確認しているエンドポイントテストで保証する。項目13の実プロセステストでは、配布用の入口から空きポートで起動し、標準出力に出たURLへのGETが200となるところまで見る。これで起動と実HTTPの接続も確認できる。[エンドポイントテスト](../tests/backends/node/healthEndpoint.test.ts#L4) · [起動テスト](../tests/backends/node/serverStartup.test.ts#L6)

### タブ選択の伝達と、選択後の記録状態を別の境界で確認する

* BlazorのAppで開始から終了ボタン・一覧件数まで再現するケースは、TimeRecorderの接続とViewModelの状態テストに対応する。Appに残すのはタブ選択が記録欄の対象切替に届く代表例と、タブの選択表示・見出し。記録中か、他のタブの記録が変化するかはViewModelへ集約する。Reactも、Aへの切替で同じ見出しを確認する単独ケースを、項目03の未指定→A→未指定のテストに寄せ、Aを選択した時点で両タブの `aria-selected` と見出しを確認する。Appから記録欄へ接続する代表経路を残す。[BlazorのApp](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L202) · [ReactのApp](../tests/frontends/react/App.test.tsx#L130) · [Reactの状態](../tests/frontends/react/presentation/timeRecorderState.test.ts#L25)

### 記録の表示に必要な入力を直接与える

* 項目14・15・17・28と合わせる。開始・終了クリックは、それぞれ対応する操作が呼ばれ、返った記録が代表的な表示に反映される例を残す。終了クリックの前提は進行中の記録をサービスまたはViewModelの代役から渡す。再開始で終了欄が消えることは、終了済みから記録中のデータへ同じ画面を再描画して確認する。操作欄と一覧への配置は各領域の代表値で確認し、親画面のテストから子と重なる日時の書式・HTML属性の詳細を外す。[Reactの画面](../tests/frontends/react/timeRecords/TimeRecorder.test.tsx#L149) · [Blazorの画面](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecorderTests.cs#L77)
* `<time>` の値・名前・各時刻への割り当ては `TimeRecordTimes` の画面で確認する。Reactの小数秒と日付境界は `formatHtmlDateTime` の直接テストに集約する。BlazorのHTML日時の小数秒は子コンポーネントで確認し、日本時間・経過時間の書式の詳細は既存のFormatterテストで確認する。Blazorの終了済み表示で時計を進めた後だけを読むケースは、Domainによる期間の確定、子による終了済みの時計非参照と終了時のタイマー停止の確認に対応する。Reactでは固定60秒後の更新と分境界前後の更新が重なるので、より具体的な後者を残す。[Blazorの時刻表示](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecordTimesTests.cs#L64) · [Reactの時刻表示](../tests/frontends/react/timeRecords/TimeRecordTimes.test.tsx#L32) · [Reactの時計と書式](../tests/frontends/react/presentation/useTimeRecordTimes.test.ts#L70)

### 再取得のクリック、未確認表示、状態復旧を独立して観測する

* 項目16・23に集約する。Blazorの同じ再取得クリックの二件を、委譲された関数の実行前後の一覧取得回数と、開始・終了要求が行われないことを一度に確認する接続テストにする。警告と再取得可否の表示は、未確認状態を持つ代役でそれぞれ一例確認する。開始・終了ボタンの無効化は両方のボタンを確認する。Reactも画面には再取得の代表的な接続を残し、空一覧からの再開と再失敗はhook・Reducerで確認する。再失敗までの一覧要求三回は現行hookで確認済み。空一覧で次の開始が一度届く確認は、画面から外す前にhookへ移す。再取得中の排他ケースは、未確認状態の時点で開始・終了が既に抑止されるため、保留中の再取得を連打しても通信が増えないことを直接観測する。開始・終了に対する排他を検証する場合は、他の抑止条件がない前提で行う。[Blazor再取得](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecorderTests.cs#L173) · [React再取得](../tests/frontends/react/timeRecords/TimeRecorder.test.tsx#L177) · [React hook](../tests/frontends/react/presentation/useTimeRecorder.test.ts#L219)

### 古い一覧の到着で何が変わるかを直接確認する

* 項目21に集約する。Blazorでは古い応答を受けた直後の未確認状態を、追加の開始・終了操作の前に確認する。該当操作が抑止されることは直後の専用テストが保証する。Reactでも応答が届くテストの本文に保留応答の解決を置く。古い応答の後に終了するケースは、正しい終了対象IDが要求されたことを中心に確認する。一般的な終了後の状態はhookとReducerの終了テストに対応する。[BlazorのViewModel](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L494) · [Reactのhook](../tests/frontends/react/presentation/useTimeRecorder.test.ts#L483)

### 通信から復元した記録のクラスとDomainの終了動作を確認する

* 項目22に集約する。Reactの通信テストでは、期待する `TimeRecord` の一覧との `toStrictEqual` でクラス・値・順序を保証する。通信した後に `.end()` を呼び、元の記録・ID・開始時刻・新しい終了時刻を改めて照合するケースはDomainの直接テストで対応できる。[通信による復元](../tests/frontends/react/infrastructure/timeRecordService.test.ts#L33) · [Domainの終了](../tests/domains/typescript/timeRecord.test.ts#L28)

### Appにはログアウトと正常性表示の接続を残す

* Reactでログアウト後に一覧が消えるテストは、一覧が表示された認証済み状態で十分。前提の開始操作は、再ログイン後に前の記録中状態が消えることを検証するケースで使う。Blazorのセッション切替も初回のサービスに記録中の応答、次回に空の応答を直接与えられる。SDKコールバックと代替ボタン、初期注入された認証状態と通常ログインはそれぞれ入口が異なるため、項目04・27の代表経路を維持する。[Reactのログアウト](../tests/frontends/react/App.test.tsx#L46) · [Blazorのセッション切替](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L108)
* BlazorのAppでは正常性表示がフッターに配置されることを観測する。確認中から正常／失敗に変わる日本語の表示は `HealthStatus` のテストで保証する。[Appのフッター](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs#L146) · [HealthStatusのテスト](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/Health/HealthStatusTests.cs#L20)

**反映結果（2026-11-20）**

基準は `21bf4a6`。12項目をテストへ反映し、バックエンド・Domainを `f33b9df`、フロントエンドと本記録を `5ef1933` でコミットした。変更時点の.NET全300テスト、非増分ビルド、format、TypeScript全221テスト、ビルド、lintが成功。項目13〜17は対応済み。修正一覧の次は項目18。

C# 15への更新の基準は `5ef1933`。.NET 11にプロジェクト・依存パッケージ・起動スクリプト・READMEを更新した。一時領域のSDK 11.0.100で全.NETテスト300件とビルドが成功。既存の `GoogleSignIn.cs` に新しいBL0016警告が1件あり、format検査は終了コード2。Visual Studioを18.10から18.12へ更新し、付属MSBuildでnet11.0・C# 15.0とDomainのビルドを確認した。インストーラーは再起動が必要な正常終了（3010）を返した。通常環境へのSDK 11.0.100インストールは成功し、dotnetコマンドでC# 15.0を確認済み。

変更したテストは次の18ファイル。

* バックエンドとDomain　[TimeRecordEndpointTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api.Tests/Endpoints/TimeRecords/TimeRecordEndpointTests.cs) · [TimeRecordEndOperationTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordEndOperationTests.cs) · [TimeRecordStartOperationTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordStartOperationTests.cs) · [InMemoryTimeRecordStateTests.cs](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Infrastructure.Tests/TimeRecords/InMemoryTimeRecordStateTests.cs) · [timeRecordEndpoints.test.ts](../tests/backends/node/api/timeRecordEndpoints.test.ts) · [timeRecords.test.ts](../tests/backends/node/application/timeRecords.test.ts) · [serverStartup.test.ts](../tests/backends/node/serverStartup.test.ts) · [TimeRecordTests.cs](../tests/domains/dotnet/Kizamu.Domain.Tests/TimeRecordTests.cs)
* Blazor　[AppTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs) · [TimeRecordTimesTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecordTimesTests.cs) · [TimeRecorderTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecorderTests.cs) · [TimeRecorderViewModelTests.cs](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs)
* React　[App.test.tsx](../tests/frontends/react/App.test.tsx) · [timeRecordService.test.ts](../tests/frontends/react/infrastructure/timeRecordService.test.ts) · [useTimeRecordTimes.test.ts](../tests/frontends/react/presentation/useTimeRecordTimes.test.ts) · [useTimeRecorder.test.ts](../tests/frontends/react/presentation/useTimeRecorder.test.ts) · [TimeRecordTimes.test.tsx](../tests/frontends/react/timeRecords/TimeRecordTimes.test.tsx) · [TimeRecorder.test.tsx](../tests/frontends/react/timeRecords/TimeRecorder.test.tsx)

## 仕様の決定が必要な差

| 条件 | 現在の結果 | 決まった後の変更単位 |
| --- | --- | --- |
| 初回一覧の応答に含まれない記録を、取得中にA開始→B開始→A終了の順に操作する | BlazorはA・Bの開始順。ReactはB・Aの最終更新順。両側にそれぞれの順序を固定するテストがある。 | 順序の決定が必要。開始順ならReactの更新Mapへの再挿入をやめ、最終更新順ならBlazorの合成順を変える。どちらもAは最新の終了済み一件、Bは未終了一件となることを同じ入力で確認する。[TimeRecorderViewModel.cs 111行](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs#L111) · [TimeRecorderViewModelTests.cs 813行](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L813) · [timeRecorderState.ts 73行](../src/frontends/react/src/presentation/timeRecorderState.ts#L73) · [timeRecorderState.test.ts 206行](../tests/frontends/react/presentation/timeRecorderState.test.ts#L206) |
| サーバーから初めて受けた未終了記録にプロジェクト情報がない | この版の通信契約には記録の所属がない。02で再現したのは、画面自身がAで開始し、既に所属が分かる記録。 | 再ログインや再読込をまたぐ所属の復元は、プロジェクトと記録の保存の仕様が決まってから扱う。02は独立して修正できる。 [ロードマップ](../docs/roadmap.md) |
| 開始・終了の応答待ちにタブを切り替えたとき、操作欄にどの時刻を表示するか | Aを終了したときの開始・終了時刻は共にAの記録から取る方針が確定済み。終了対象IDと直近の表示記録は別の値。選択中のプロジェクトだけの履歴を表示するか、直近の操作結果を表示するかで期待結果が変わる。Reactには別タブで開始応答を受けた例もある。 | 表示方針を決める場合は、Aの応答をBで受けた時点とAに戻った時点の期待表示を別途指定する。01・02はどちらの表示方針でも必要。 |

初回一覧取得自体の失敗後の表示はD012の対象外。サービス差し替え後に届いた古い一覧の扱いは既存候補C003を参照する。[判断記録](../docs/decisions.md) · [開発候補](development-candidates.md)

## 現状のまま維持する比較

TS Domainの不変性、.NETの値等価、通信でのクラス復元、参照同一性は別の保証になっている。依存中のVitestとTemporal実装で、同じ日時点の別ISO表記は `should.deep.equal`・`should.toStrictEqual` と入れ子の厳密比較で一致し、100ナノ秒の差は三通りとも不一致になることを確認した。JSONの値比較はその目的に合う既存の比較を維持する。元のISO表記、HTMLのミリ秒文字列、期待クラス、同じインスタンスであることを明示した検証も残す。[timeRecordService.test.ts 130行](../tests/frontends/react/infrastructure/timeRecordService.test.ts#L130) · [timeRecorderState.test.ts 195行](../tests/frontends/react/presentation/timeRecorderState.test.ts#L195) · [TimeRecordTimes.test.tsx 16行](../tests/frontends/react/timeRecords/TimeRecordTimes.test.tsx#L16)

## 公開APIの契約の全体精査

2026-11-20、`4e04843` を基準に、公開操作の入力、呼び出し側に求める前提、確認できる結果を、利用先・仕様・既存テストと照合した。対象は製品、テスト、共通補助、起動スクリプトの全195ファイル、10,660行。自動生成の依存ロックやビルド出力は通読対象外。

| 範囲 | ファイル | 行 |
| --- | ---: | ---: |
| C#・TypeScriptのDomain、起動スクリプト、共通テスト補助 | 26 | 686 |
| ASP.NET Core・Nodeと対応テスト | 59 | 2,511 |
| Blazorと対応テスト | 56 | 4,060 |
| Reactと対応テスト | 54 | 3,403 |

### 確定仕様との差

- **修正済み。終了の失敗後の再取得で、別タブの終了対象を更新する。** 進行中のAとBがあり、Bの終了応答を待つ間にAに切り替える。従来は応答が失敗し、再取得でBの終了を確認しても、Bに戻ると終了を再送できた。D009では再終了が終了時刻を更新するため、意図せず時刻を変えられる。D012に従い、再取得した一覧で終了済みと確認できた別タブの対象を解除した。進行中の既知対象と、所属不明の記録に対する既存の割当は維持する。 [D012](../docs/decisions.md#d012記録操作の結果が未確認なら一覧の再取得で復旧する)
- **同じ対象に開始要求を繰り返せる。** ViewModelとhookは操作中や未確認の状態を抑止する一方、開始が成功した後の同じ対象には再度送信できる。画面は開始ボタンを切り替える。操作の境界でも既に進行中なら二件目は作れないことを、同じタブへの連続呼び出しで検証する。抑止結果を呼び出し側にどう伝えるか、初回一覧取得と並行する開始の扱いは別途決める。 [Blazor](../src/frontends/blazor/Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs#L150) · [React](../src/frontends/react/src/presentation/useTimeRecorder.ts#L79) · [MVPの同時記録の条件](../docs/roadmap.md#ステップ1-プロジェクト単位の複数同時記録)
- **終了の成功応答が操作の結果を保証しない。** 両画面の通信サービスは、2xxなら別IDの記録や終了時刻が `null` の記録も返す。主なサーバー経路は正しい応答を返す。通信境界のテストに、別ID、終了時刻なし、正常な終了を与え、受理するのは指定IDの終了済み記録だけにする。Reactでは復元前に時刻のJSON型も確認する。 [Blazor](../src/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure/TimeRecords/TimeRecordApi.cs#L33) · [React](../src/frontends/react/src/infrastructure/timeRecordService.ts#L27)
- **修正済み。保存APIの差し替えで対象の不在を通知する。** ASP.NET Coreの `Update` とNodeの `replace` は、未保存IDの更新時に既存の専用例外を投げる。保存テストで、失敗時に既存記録と保存順が保たれ、新規追加もないことを確かめた。 [C#](../src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Application/TimeRecords/ITimeRecordState.cs#L33) · [Node](../src/backends/node/application/timeRecords.ts#L4)
- **認証結果の境界条件が実装によって異なる。** 採用済みライブラリの期限判定の猶予が両バックエンドで異なり、現在の期限テストは十分に経過した例を使う。また、本人識別子の形式や公開鍵取得に失敗した場合の契約を揃える余地がある。D014の期限・本人識別の契約を基準に、時計の境界値と検証器の失敗をテストする。 [D014](../docs/decisions.md#d014ベータ版はgoogleのidトークンで本人を識別する) · [Node検証器](../src/backends/node/infrastructure/googleIdTokenVerifier.ts#L14)
- **起動確認が待機せず失敗する条件がある。** 項目30を、204のあと待機を挟めば200を返す代役で確認した。現行は待機0回で60回照会し失敗を返す。項目31の起動失敗時の診断表示も確認した。修正箇所とテスト案は項目30・31に記載済み。 [Wait-HealthEndpoint.ps1](../scripts/Wait-HealthEndpoint.ps1#L7)

### 修正と検証

基準は `4e04843`。画面を介さないViewModelとhookのテストで、期待した終了要求 `[B, A]` に対し `[B, B, A]` となる失敗を両方で確認した。最小修正後は両テストが成功し、.NET 303件とnpm 225件がすべて成功。両方のビルド、ESLint、変更したC#ファイルのformatと差分検査も成功した。.NETの全体formatは既知の `GoogleSignIn.cs` のBL0016で失敗。修正は `b0017be` に記録済み。保存先の差し替えについては下記に記録。 [Blazorのテスト](../tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs#L252) · [Reactのテスト](../tests/frontends/react/presentation/useTimeRecorder.test.ts#L570)

### 仕様を決めてから扱う事項

- 初回の一覧取得が失敗した場合はD012の対象外。Reactでは未処理の拒否となり、Blazorでは初期表示時の例外になる。空一覧と区別する表示、再取得の方法、初回取得中の操作可否を決める。サービス切替と古い応答は既存候補C003を参照する。 [D012](../docs/decisions.md#d012記録操作の結果が未確認なら一覧の再取得で復旧する) · [候補一覧](development-candidates.md)
- 所属不明の記録をどのタブに割り当てるか、どの記録を操作欄に表示するかは「仕様の決定が必要な差」を参照する。既知の割当に対する優先順も、新しい割当と区別して扱う。
- 保存先に既にあるIDを再度追加した場合の責務、取得済み一覧が後の更新を反映するかは保存契約の論点。C#の `All` のライブ性と `CopyAll` の固定スナップショットは既存テストが保証する。Nodeの取得済み一覧のライブ性に同じ保証はない。終了時刻が開始時刻より前になる入力はロードマップの未決事項にある。 [C#の保存テスト](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Infrastructure.Tests/TimeRecords/InMemoryTimeRecordStateTests.cs#L79) · [ロードマップ](../docs/roadmap.md#ステップ3-記録の修正)

### 保存APIの差し替え

2026-11-20、基準は `5c24bd1`。両保存実装の公開操作に、対象不在の例外と失敗後の一覧を確認するテストを追加。実装前に双方が例外を投げず失敗することを確認した。修正後はASP.NET Core側85件、npm側231件、両ビルド、format、Lint、差分検査が成功。 [ASP.NET Coreのテスト](../tests/backends/aspnetcore/Kizamu.Backend.AspNetCore.Infrastructure.Tests/TimeRecords/InMemoryTimeRecordStateTests.cs#L69) · [Nodeのテスト](../tests/backends/node/infrastructure/timeRecordInfrastructure.test.ts#L37)

## TypeScriptの型と実行時検査

2026-11-20、基準は `ae8482a`。TypeScript・JavaScriptの70ファイルと型設定8件を精査した。Mapの有無を一度だけ判定し、開始・終了のアクションでは操作元を必須にした。オブジェクトの有無は真偽値で判定し、記録IDのnull判定は維持した。プロジェクト選択テストには既知のProjectを渡す。

Reactでは記録通信のJSONをunknownから検証し、Nodeでは外部の本人識別子が空でない文字列の場合に返す。実装前は時刻の型と本人識別子に関する4件が期待どおり失敗した。配列以外の一覧は既存実装でも拒否されていた。実装後は全230テスト、ビルド、Lint、差分検査が成功。分岐の整理は `0e16d2a` に記録済み。

## 基準コミットで実行した検証

| 確認 | 結果 |
| --- | --- |
| `dotnet test Kizamu.Blazor.slnx --no-restore` | 8テストプロジェクト、325件成功、失敗・スキップ0件 |
| ルートの `npm test` | Domain 17件、React 163件、Node 58件、計238件成功、失敗・スキップ0件 |
| .NETの非増分ビルドとアナライザー | 16プロジェクト、警告・エラー0件。生成されたSARIF 16件の診断0件 |
| `dotnet format Kizamu.Blazor.slnx --no-restore --verify-no-changes --severity info` | 成功 |
| ルートの `npm run lint`、`npm run build` | 両方成功 |

01のReact側は実際のReducer、02はBlazorの公開ViewModelとReactのReducerで応答順を変えて確認した。これらは563件の既存テストに加え、今回のレビュー中に行った切り分け。実ブラウザーでの操作、実Googleアカウントでのログイン、修正後の検証は未実施。

## 通読したファイル

折りたたみ内は基準コミットの全ファイル。番号は変更・検証・照合で参照する指示を示す。「維持」は今回そのファイル単独に対する修正指示がないことを示す。

<details>
<summary>scripts　3ファイル・109行</summary>

| ファイル | 指示 |
| --- | --- |
| `Check-Blazor.ps1` | 30・31 |
| `Check-React.ps1` | 30・31 |
| `Wait-HealthEndpoint.ps1` | 30 |

</details>

<details>
<summary>src/backends/aspnetcore　20ファイル・571行</summary>

| ファイル | 指示 |
| --- | --- |
| `Kizamu.Backend.AspNetCore.Api/Endpoints/Health/HealthEndpoint.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Api/Endpoints/TimeRecords/EndTimeRecordEndpoint.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Api/Endpoints/TimeRecords/GetTimeRecordsEndpoint.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Api/Endpoints/TimeRecords/StartTimeRecordEndpoint.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Api/Endpoints/TimeRecords/TimeRecordAuthentication.cs` | 07 |
| `Kizamu.Backend.AspNetCore.Api/Endpoints/TimeRecords/TimeRecordResponse.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Api/Kizamu.Backend.AspNetCore.Api.csproj` | 維持 |
| `Kizamu.Backend.AspNetCore.Api/Kizamu.Backend.AspNetCore.Api.http` | 維持 |
| `Kizamu.Backend.AspNetCore.Api/Program.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Api/Properties/launchSettings.json` | 維持 |
| `Kizamu.Backend.AspNetCore.Api/appsettings.Development.json` | 維持 |
| `Kizamu.Backend.AspNetCore.Api/appsettings.json` | 維持 |
| `Kizamu.Backend.AspNetCore.Application/Kizamu.Backend.AspNetCore.Application.csproj` | 維持 |
| `Kizamu.Backend.AspNetCore.Application/TimeRecords/ITimeRecordState.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Application/TimeRecords/TimeRecordEndOperation.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Application/TimeRecords/TimeRecordReader.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Application/TimeRecords/TimeRecordStartOperation.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Infrastructure/Authentication/GoogleIdTokenVerifier.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Infrastructure/Kizamu.Backend.AspNetCore.Infrastructure.csproj` | 維持 |
| `Kizamu.Backend.AspNetCore.Infrastructure/TimeRecords/InMemoryTimeRecordState.cs` | 維持 |

</details>

<details>
<summary>src/backends/node　12ファイル・252行</summary>

| ファイル | 指示 |
| --- | --- |
| `.vscode/launch.json` | 維持 |
| `api/timeRecordEndpoints.ts` | 07 |
| `app.ts` | 維持 |
| `application/timeRecords.ts` | 維持 |
| `eslint.config.js` | 維持 |
| `infrastructure/googleIdTokenVerifier.ts` | 08 |
| `infrastructure/timeRecordSources.ts` | 維持 |
| `infrastructure/timeRecordState.ts` | 維持 |
| `kizamu-backend-node.esproj` | 維持 |
| `package.json` | 維持 |
| `server.ts` | 維持 |
| `tsconfig.json` | 維持 |

</details>

<details>
<summary>src/domains/dotnet　3ファイル・74行</summary>

| ファイル | 指示 |
| --- | --- |
| `Kizamu.Domain/Kizamu.Domain.csproj` | 維持 |
| `Kizamu.Domain/Project.cs` | 維持 |
| `Kizamu.Domain/TimeRecord.cs` | 維持 |

</details>

<details>
<summary>src/domains/typescript　7ファイル・88行</summary>

| ファイル | 指示 |
| --- | --- |
| `eslint.config.js` | 維持 |
| `index.ts` | 維持 |
| `kizamu-domain-typescript.esproj` | 維持 |
| `package.json` | 維持 |
| `project.ts` | 維持 |
| `timeRecord.ts` | 維持 |
| `tsconfig.json` | 維持 |

</details>

<details>
<summary>src/frontends/blazor　31ファイル・1,201行</summary>

| ファイル | 指示 |
| --- | --- |
| `Kizamu.Frontend.Blazor.Client/App.razor` | 03・04・05・27 |
| `Kizamu.Frontend.Blazor.Client/GoogleSignIn.cs` | 05 |
| `Kizamu.Frontend.Blazor.Client/Health/HealthStatus.razor` | 維持 |
| `Kizamu.Frontend.Blazor.Client/IGoogleSignIn.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Client/Kizamu.Frontend.Blazor.Client.csproj` | 維持 |
| `Kizamu.Frontend.Blazor.Client/Program.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Client/TimeRecords/HtmlDateTimeExtensions.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecordTimes.razor` | 維持 |
| `Kizamu.Frontend.Blazor.Client/TimeRecords/TimeRecorder.razor` | 16 |
| `Kizamu.Frontend.Blazor.Client/_Imports.razor` | 維持 |
| `Kizamu.Frontend.Blazor.Client/wwwroot/app.css` | 29 |
| `Kizamu.Frontend.Blazor.Client/wwwroot/google-sign-in.js` | 維持 |
| `Kizamu.Frontend.Blazor.Client/wwwroot/index.html` | 維持 |
| `Kizamu.Frontend.Blazor.Infrastructure/Health/HealthApi.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Infrastructure/Kizamu.Frontend.Blazor.Infrastructure.csproj` | 維持 |
| `Kizamu.Frontend.Blazor.Infrastructure/TimeRecords/TimeRecordApi.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Infrastructure/TimeRecords/TimeRecordResponse.cs` | 06 |
| `Kizamu.Frontend.Blazor.Presentation/Health/HealthStatusViewModel.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation/Health/IHealthService.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation/Kizamu.Frontend.Blazor.Presentation.csproj` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation/Projects/ProjectTabsViewModel.cs` | 03 |
| `Kizamu.Frontend.Blazor.Presentation/TimeRecords/ITimeRecordService.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation/TimeRecords/ITimeRecorderViewModel.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecordRefreshTimer.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecordTimesFormatter.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderNotice.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation/TimeRecords/TimeRecorderViewModel.cs` | 01・02 |
| `Kizamu.Frontend.Blazor.Server/Kizamu.Frontend.Blazor.Server.csproj` | 維持 |
| `Kizamu.Frontend.Blazor.Server/Program.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Server/Properties/launchSettings.json` | 維持 |
| `Kizamu.Frontend.Blazor.Server/appsettings.json` | 維持 |

</details>

<details>
<summary>src/frontends/react　29ファイル・1,271行</summary>

| ファイル | 指示 |
| --- | --- |
| `eslint.config.js` | 維持 |
| `index.html` | 維持 |
| `kizamu-frontend-react.esproj` | 維持 |
| `package.json` | 維持 |
| `src/App.tsx` | 03・04・27 |
| `src/googleSignIn.ts` | 維持 |
| `src/health/HealthStatus.tsx` | 維持 |
| `src/index.css` | 29 |
| `src/infrastructure/healthService.ts` | 25 |
| `src/infrastructure/timeRecordService.ts` | 06 |
| `src/main.tsx` | 維持 |
| `src/presentation/healthService.ts` | 維持 |
| `src/presentation/pendingTimeRecordLoads.ts` | 02 |
| `src/presentation/projectTabsState.ts` | 03・27 |
| `src/presentation/timeRecordPresentation.ts` | 28 |
| `src/presentation/timeRecordService.ts` | 維持 |
| `src/presentation/timeRecorderOperationState.ts` | 維持 |
| `src/presentation/timeRecorderState.ts` | 01・02 |
| `src/presentation/useHealthStatus.ts` | 維持 |
| `src/presentation/useTimeRecordTimes.ts` | 維持 |
| `src/presentation/useTimeRecorder.ts` | 01・18 |
| `src/timeRecords/TimeRecordTimes.tsx` | 28 |
| `src/timeRecords/TimeRecorder.tsx` | 維持 |
| `src/timeRecords/htmlDateTime.ts` | 維持 |
| `src/vite-env.d.ts` | 維持 |
| `tsconfig.app.json` | 維持 |
| `tsconfig.json` | 維持 |
| `tsconfig.node.json` | 維持 |
| `vite.config.ts` | 維持 |

</details>

<details>
<summary>tests（共通）　2ファイル・17行</summary>

| ファイル | 指示 |
| --- | --- |
| `TestSupport/TestCancellation.cs` | 維持 |
| `should.mts` | 維持 |

</details>

<details>
<summary>tests/backends/aspnetcore　15ファイル・1,094行</summary>

| ファイル | 指示 |
| --- | --- |
| `Kizamu.Backend.AspNetCore.Api.Tests/ApiDependenciesTests.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Api.Tests/BlazorHostingTests.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Api.Tests/HealthEndpointTests.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Api.Tests/Kizamu.Backend.AspNetCore.Api.Tests.csproj` | 維持 |
| `Kizamu.Backend.AspNetCore.Api.Tests/TimeRecordEndpointTests.cs` | 07・10・11・26・32 |
| `Kizamu.Backend.AspNetCore.Application.Tests/ApplicationDependenciesTests.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Application.Tests/Kizamu.Backend.AspNetCore.Application.Tests.csproj` | 維持 |
| `Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/FakeTimeRecordState.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordEndOperationTests.cs` | 09 |
| `Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordReaderTests.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Application.Tests/TimeRecords/TimeRecordStartOperationTests.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Infrastructure.Tests/Authentication/GoogleIdTokenVerifierTests.cs` | 11・26 |
| `Kizamu.Backend.AspNetCore.Infrastructure.Tests/InfrastructureDependenciesTests.cs` | 維持 |
| `Kizamu.Backend.AspNetCore.Infrastructure.Tests/Kizamu.Backend.AspNetCore.Infrastructure.Tests.csproj` | 維持 |
| `Kizamu.Backend.AspNetCore.Infrastructure.Tests/TimeRecords/InMemoryTimeRecordStateTests.cs` | 維持 |

</details>

<details>
<summary>tests/backends/node　11ファイル・598行</summary>

| ファイル | 指示 |
| --- | --- |
| `eslint.config.js` | 維持 |
| `googleIdTokenVerifier.test.ts` | 08・11・26 |
| `healthEndpoint.test.ts` | 維持 |
| `kizamu-backend-node-tests.esproj` | 維持 |
| `package.json` | 維持 |
| `serverStartup.test.ts` | 13 |
| `timeRecordEndpoints.test.ts` | 07・10・11・26・32 |
| `timeRecordInfrastructure.test.ts` | 維持 |
| `timeRecords.test.ts` | 12 |
| `tsconfig.json` | 維持 |
| `vitest.config.ts` | 維持 |

</details>

<details>
<summary>tests/domains/dotnet　4ファイル・184行</summary>

| ファイル | 指示 |
| --- | --- |
| `Kizamu.Domain.Tests/DomainDependenciesTests.cs` | 維持 |
| `Kizamu.Domain.Tests/Kizamu.Domain.Tests.csproj` | 維持 |
| `Kizamu.Domain.Tests/ProjectTests.cs` | 維持 |
| `Kizamu.Domain.Tests/TimeRecordTests.cs` | 維持 |

</details>

<details>
<summary>tests/domains/typescript　7ファイル・204行</summary>

| ファイル | 指示 |
| --- | --- |
| `dependencyBoundaries.test.ts` | 19 |
| `eslint.config.js` | 維持 |
| `kizamu-domain-typescript-tests.esproj` | 維持 |
| `package.json` | 維持 |
| `timeRecord.test.ts` | 22 |
| `tsconfig.json` | 維持 |
| `vitest.config.ts` | 維持 |

</details>

<details>
<summary>tests/frontends/blazor　24ファイル・2,842行</summary>

| ファイル | 指示 |
| --- | --- |
| `Kizamu.Frontend.Blazor.Client.Tests/AppTests.cs` | 03・04・15・20・32 |
| `Kizamu.Frontend.Blazor.Client.Tests/GoogleSignInTests.cs` | 05・32 |
| `Kizamu.Frontend.Blazor.Client.Tests/Health/HealthStatusTests.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Client.Tests/Kizamu.Frontend.Blazor.Client.Tests.csproj` | 維持 |
| `Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecordRefreshTimerTests.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecordTimesTests.cs` | 17 |
| `Kizamu.Frontend.Blazor.Client.Tests/TimeRecords/TimeRecorderTests.cs` | 14・16 |
| `Kizamu.Frontend.Blazor.Infrastructure.Tests/Health/HealthApiTests.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Infrastructure.Tests/InfrastructureDependenciesTests.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Infrastructure.Tests/Kizamu.Frontend.Blazor.Infrastructure.Tests.csproj` | 維持 |
| `Kizamu.Frontend.Blazor.Infrastructure.Tests/TimeRecords/TimeRecordApiTests.cs` | 06 |
| `Kizamu.Frontend.Blazor.Infrastructure.Tests/TimeRecords/TimeRecordAuthorizationTests.cs` | 24 |
| `Kizamu.Frontend.Blazor.Presentation.Tests/Health/HealthStatusViewModelTests.cs` | 32 |
| `Kizamu.Frontend.Blazor.Presentation.Tests/Kizamu.Frontend.Blazor.Presentation.Tests.csproj` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation.Tests/PresentationDependenciesTests.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation.Tests/Projects/ProjectTabsViewModelTests.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecordTimesFormatterTests.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Presentation.Tests/TimeRecords/TimeRecorderViewModelTests.cs` | 01・02・21・32 |
| `Kizamu.Frontend.Blazor.Server.Tests/Hosting/ApiProxyTests.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Server.Tests/Hosting/BlazorHostingTests.cs` | 維持 |
| `Kizamu.Frontend.Blazor.Server.Tests/Kizamu.Frontend.Blazor.Server.Tests.csproj` | 維持 |
| `TestSupport/StubHealthService.cs` | 維持 |
| `TestSupport/StubTimeRecordService.cs` | 維持 |
| `TestSupport/TestHttpMessageHandler.cs` | 維持 |

</details>

<details>
<summary>tests/frontends/react　25ファイル・2,122行</summary>

| ファイル | 指示 |
| --- | --- |
| `App.test.tsx` | 03・04・20・27・32 |
| `architecture/eslintBoundaries.test.ts` | 19 |
| `eslint.config.js` | 維持 |
| `health/HealthStatus.test.tsx` | 維持 |
| `infrastructure/healthService.test.ts` | 維持 |
| `infrastructure/timeRecordAuthorization.test.ts` | 24 |
| `infrastructure/timeRecordService.test.ts` | 06・22・28 |
| `kizamu-frontend-react-tests.esproj` | 維持 |
| `package.json` | 維持 |
| `presentation/pendingTimeRecordLoads.test.ts` | 維持 |
| `presentation/projectTabsState.test.ts` | 維持 |
| `presentation/timeRecorderOperationState.test.ts` | 23 |
| `presentation/timeRecorderState.test.ts` | 02・14・28 |
| `presentation/useHealthStatus.test.ts` | 維持 |
| `presentation/useTimeRecordTimes.test.ts` | 維持 |
| `presentation/useTimeRecorder.test.ts` | 01・18・23 |
| `startup.test.tsx` | 25 |
| `test/createTimeRecordServiceStub.ts` | 維持 |
| `test/deferred.ts` | 維持 |
| `test/setup.ts` | 維持 |
| `test/timeRecords.ts` | 維持 |
| `timeRecords/TimeRecordTimes.test.tsx` | 28 |
| `timeRecords/TimeRecorder.test.tsx` | 14・15・23 |
| `tsconfig.json` | 維持 |
| `vitest.config.ts` | 維持 |

</details>
