# Kizamu

作業を中断せず、プロジェクトごとの時間を素早く記録するタイムトラッカー。

現在、両画面はGoogle Identity Servicesのログインボタンを表示し、取得したIDトークンを記録APIへ送る構成です。2026-11-17にWindowsのChrome・localhostで、Blazor＋ASP.NET Core、React＋ASP.NET Core、React＋Nodeの実Googleログイン、記録の開始・終了、ログアウトを確認しました。確認対象のコードは327e58bで、main統合後の実Googleログイン操作は再実行していません。Blazor＋Nodeは確認未完了です。記録は業務APIのメモリ上だけに保持し、そのサーバーを再起動すると失われます。

Googleログインの準備は、[Googleログインのローカル設定](#googleログインのローカル設定)を参照してください。クライアントIDの入手、クローン後に作るファイル、APIへ渡す環境変数を説明しています。

開始・終了に失敗すると、失敗を表示して一覧を再取得します。その再取得にも失敗した場合は、最後に確認できた一覧を残して「最新状態を確認できません」と表示します。「再取得」で状態を確認できるまで開始・終了は停止し、再取得に成功すると操作を再開できます。

MVPの第一実装はASP.NET CoreとBlazor WebAssemblyです。既存の時間記録機能をBlazorへ移し、Reactは現状を保持して追加開発を保留します。

製品構想は [docs/concept.md](docs/concept.md)、MVPまでの暫定的な順序は [docs/roadmap.md](docs/roadmap.md)、具体的な作業計画は [ai-notes/mvp-implementation-plan.md](ai-notes/mvp-implementation-plan.md) を参照してください。

開発を再開するときは、[AIの作業記録一覧](ai-notes/README.md)を入口に、[現在の次作業](ai-notes/mvp-implementation-plan.md#現在の次作業)、[判断の理由](docs/decisions.md)、[未確定の開発候補](ai-notes/development-candidates.md)を確認します。AWSのAI-DLCから、必要な仕様を対話で引き出し、合意と根拠を残して次回へ引き継ぐ考え方を取り入れています。日々の実装は既存のTDD手順に従います。

## 技術構成

- .NET 11 / ASP.NET Core Minimal API / Blazor WebAssembly
- YARP（Blazor配信サーバーから業務APIへのHTTP中継）
- xUnit.net v3 / bUnit / AwesomeAssertions
- React / TypeScript（strict）/ Vite
- Node.js / Fastify（業務APIの別実装）
- Vitest / React Testing Library / ESLint / npm

## リポジトリ構成

単一リポジトリで、最上位の `src/` と `tests/` を分け、その中を役割と実装技術で整理します。現在存在する構成は次のとおりです。

```text
kizamu/
├─ src/
│  ├─ backends/
│  │  ├─ aspnetcore/
│  │  │  ├─ Kizamu.Backend.AspNetCore.Api/
│  │  │  ├─ Kizamu.Backend.AspNetCore.Application/
│  │  │  └─ Kizamu.Backend.AspNetCore.Infrastructure/
│  │  └─ node/                      kizamu-backend-node・業務APIのみ
│  │     ├─ api/                    HTTPルート・応答DTO変換
│  │     ├─ application/            記録操作・依存契約
│  │     ├─ infrastructure/         メモリ保持・時刻・ID生成
│  │     └─ app.ts                  アプリの組み立て
│  ├─ frontends/
│  │  ├─ blazor/
│  │  │  ├─ Kizamu.Frontend.Blazor.Client/   第一実装・WebAssembly UI
│  │  │  ├─ Kizamu.Frontend.Blazor.Presentation/   記録画面の状態・操作とサービス契約
│  │  │  ├─ Kizamu.Frontend.Blazor.Infrastructure/ HTTP通信・応答DTO
│  │  │  └─ Kizamu.Frontend.Blazor.Server/   画面配信・API中継
│  │  └─ react/                     既存React実装・追加開発は保留
│  │     ├─ kizamu-frontend-react.esproj
│  │     ├─ package.json
│  │     └─ src/
│  └─ domains/
│     ├─ dotnet/
│     │  └─ Kizamu.Domain/
│     └─ typescript/                kizamu-domain-typescript・Node/React共用
├─ tests/
│  ├─ TestSupport/                  .NET実装間で共有するテスト補助（ソースリンク）
│  ├─ backends/
│  │  ├─ aspnetcore/
│  │  │  ├─ Kizamu.Backend.AspNetCore.Api.Tests/
│  │  │  ├─ Kizamu.Backend.AspNetCore.Application.Tests/
│  │  │  └─ Kizamu.Backend.AspNetCore.Infrastructure.Tests/
│  │  └─ node/                      kizamu-backend-node-tests
│  ├─ frontends/
│  │  ├─ blazor/
│  │  │  ├─ Kizamu.Frontend.Blazor.Client.Tests/
│  │  │  ├─ Kizamu.Frontend.Blazor.Presentation.Tests/
│  │  │  ├─ Kizamu.Frontend.Blazor.Infrastructure.Tests/
│  │  │  ├─ TestSupport/             テスト間で共有する小さなスタブ（ソースリンク）
│  │  │  └─ Kizamu.Frontend.Blazor.Server.Tests/
│  │  └─ react/
│  │     ├─ kizamu-frontend-react-tests.esproj
│  │     ├─ package.json
│  │     ├─ vitest.config.ts
│  │     └─ …                       既存テストと補助コード
│  └─ domains/
│     ├─ dotnet/
│     │  └─ Kizamu.Domain.Tests/
│     └─ typescript/                kizamu-domain-typescript-tests
├─ docs/                            製品仕様・設計判断・開発文書・作業指示
├─ ai-notes/                        AIの作業計画・検討候補・引継ぎ
├─ Kizamu.slnx                      全実装の作業用
├─ Kizamu.Blazor.slnx               BlazorとASP.NET Core、Domain、そのテスト
├─ Kizamu.AspNetCore.slnx            ASP.NET CoreとDomain、そのテスト
├─ Kizamu.code-workspace            VS Code用の作業設定
├─ package.json                     npm workspacesと共通コマンド
└─ package-lock.json                npm依存関係の共通lockfile
```

Visual Studioのソリューション表示は、物理配置や公開時の組み合わせとは別に扱います。同じ実装のプロジェクトとテストを、`Backends/ASP.NET Core`、`Backends/Node`、`Frontends/Blazor`、`Frontends/React`、`Domains/.NET` の論理フォルダーへまとめています。BlazorとASP.NET Coreの開発には `Kizamu.Blazor.slnx`、ASP.NET CoreとDomainのテストだけの実行には `Kizamu.AspNetCore.slnx` を使います。業務APIはBlazorを参照せず、バックエンドだけでビルド・起動できます。

.NETの実装固有プロジェクトは `Kizamu.<所属>.<技術>.<役割>` と命名します。業務バックエンドのHTTP APIは `Backend.AspNetCore.Api`、Blazorのブラウザー側UIは `Frontend.Blazor.Client`、画面配信・API中継サーバーは `Frontend.Blazor.Server` とし、ClientとServerは同じUI実装内の対になる名前です。Domainと通信契約は実装技術に所属させず、それぞれ `Kizamu.Domain` と将来の `Kizamu.Api.Contracts` として区別します。

npmパッケージは各技術の習慣に合わせて小文字のハイフン区切りにし、React版は `kizamu-frontend-react`、Node版は `kizamu-backend-node` と命名します。テストは.NETで `.Tests`、npmで `-tests` を付け、`.esproj` もnpmパッケージ名に揃えます。

Domainはバックエンド専用の位置から分離し、ASP.NET CoreとBlazorから共用しています。Blazorの画面は通信DTOではなくDomainの作業記録を保持します。配置の共通化によって、すべてのフロントエンドへ同じ内部モデルの利用を強制するものではありません。

ReactとNodeの製品・テストパッケージは独立させ、ルートのnpm workspacesで管理します。依存関係のインストールとlockfileの更新はリポジトリルートで行います。各 `.esproj` による個別の自動 `npm install` は無効にしており、最初にルートで `npm ci` を実行する必要があります。

TypeScriptの作業記録モデルと期間計算は `src/domains/typescript/` の `kizamu-domain-typescript` に置き、NodeとReactから共用します。`TimeRecord` は公開プロパティが `readonly` の不変クラスで、`TimeRecord.start` で開始し、`end` は元を変更せず新しい記録を返します。既存記録はコンストラクターで復元します。`durationAt` は開始から終了までの経過時間を返し、記録中は引数に指定した時刻までを計ります。日時点には既存の `temporal-polyfill 1.0.2`（MIT）を使用します。共有DomainはReact、Fastify、Node固有API、HTTP、HTMLに依存せず、識別子の発行と現在時刻の取得はNode側に残します。Nodeは共有モデルをメモリに保持し、HTTP境界で従来のミリ秒3桁のISO文字列へ変換します。

Node版は同じ製品パッケージ内の `api/`（HTTPルート・応答DTO変換）、`application/`（開始・指定IDでの終了・一覧と依存契約）、`infrastructure/`（メモリ上の記録保持・時刻とIDの取得）に分けています。`app.ts` で依存を組み立て、記録操作は共有Domainを利用します。依存方向は `api → application / domain`、`infrastructure → application / domain`、`application → domain` です。ApplicationとInfrastructureの単体テストに加え、既存のHTTP経路の接続確認を維持しています。レイヤーは同じnpmパッケージ内のモジュールとして分けており、Node側にレイヤー間の依存を禁止する自動検査はまだありません。

React版は同じ製品パッケージ内の `src/presentation/`（画面状態・操作のフック、Reducer、サービス契約）と `src/infrastructure/`（HTTP通信・DTO変換）に責務を分けています。既存の `App`、`timeRecords/`、`health/` は表示とイベント接続を担当し、`main.tsx` で通信サービスを渡します。通信の時刻文字列は境界で共有Domainの日時点へ変換します。受信した元のISO表記はReactのPresentation側に保持します。HTMLの `datetime` 属性には、Domainの日時点から生成したUTC・ミリ秒3桁の文字列を使います。

依存方向は `表示 → presentation / domain`、`infrastructure → presentation / domain`、`presentation → domain` です。状態を持つクラスやDIコンテナではなく、Reactのフックと純粋なReducerを使用します。ESLintで型専用・未使用を含む静的importと再exportの逆依存、内側からの動的import、直接の通信・DOMグローバル参照を検出します。この検査は記載したパスと構文を対象とし、パス別名や別モジュール経由の間接呼出しを完全に追跡するものではありません。

共有DomainはESMのJavaScriptと型宣言を `dist/` へビルドし、利用側はパッケージ名でimportします。ルートのworkspacesはDomainを先に処理します。個別のReact開発・ビルド、Nodeビルド、各テストの前処理でもDomainをビルドするため、生成物がない状態から単独コマンドを実行できます。Visual Studioの `.esproj` にもDomainへのプロジェクト参照を置き、`Domains/TypeScript` に製品とテストを表示します。Nodeの起動用出力は従来どおり `npm run build --workspace=kizamu-backend-node` で作成し、`npm run start --workspace=kizamu-backend-node` で起動します。

日常の開発と差分レビューはVisual Studioを基本とし、ReactやNodeを含め、技術や組み合わせによってIDEを使い分けることは前提にしません。一方で、VS Codeでの作業も考慮します。ビルドとテストの本体は `dotnet` と `npm` のコマンドに置き、IDE固有の設定はその操作の補助として扱います。

### 今後追加する配置（未実装）

次の配置は予定です。現在はディレクトリ、生成物、空プロジェクトを作成していません。

| 配置先 | 用途・方針 |
| --- | --- |
| `contracts/openapi.yaml` | 実装言語から独立した共通HTTP API契約。複数バックエンドの互換性の基準にします。 |
| `src/api/dotnet/Kizamu.Api.Contracts/`、`src/api/typescript/` | OpenAPIから生成する通信用DTO等。TypeScriptパッケージは `kizamu-api-typescript`。Domainとは分離します。 |
| `tests/api/` | 通信コードの検証が必要になった場合のテスト。生成DTOの数だけ形式的にテストを増やしません。 |
| `tests/contracts/` | 各バックエンドが共通のHTTP API契約を満たすことを確認するテスト。 |
| `tests/system/` | フロントエンドとバックエンドを組み合わせた製品全体のテスト。 |
| `eng/` | 必要になったコード生成、ビルド、公開成果物の組み立ての補助手順。 |

通信用DTOは共通のOpenAPI契約から各言語向けに生成する方針です。API呼び出しコードの生成も検討対象とし、対象言語・ランタイムの進化への追随、必要な機能への対応速度、拡張性、実際の更新履歴と生成結果を確認して採否を決めます。生成ツールは未選定です。生成物は手書き部分と区別し、手修正せず、ツールのバージョンと設定を固定して再生成できるようにします。生成DTOと呼び出し処理のプロジェクト分割は、採用ツールの出力と依存関係を確認して決めます。

ASP.NET Coreの開始・終了・一覧取得は `Kizamu.Backend.AspNetCore.Application` に置き、HTTPを使わずに時刻の取得、記録状態の更新、一覧の読取結果への変換を確認します。終了時は指定IDで保存された記録を取得し、その記録オブジェクトを終了します。終了済み記録への再要求では終了時刻を更新し、対象外の記録は変更しません。Apiはルート・状態コード・JSON応答への変換と起動時の構成、Infrastructureはメモリ上の記録保持とID検索を担当します。依存方向は `Api → Application / Infrastructure`、`Infrastructure → Application / Domain`、`Application → Domain` です。アセンブリ参照のテストでこの方向とApplication・DomainのHTTP/JSONからの独立を確認します。対象はコンパイル結果に残る参照であり、未使用の参照設定や動的ロードまでは検出しません。

Blazorの記録画面の状態と操作は `Kizamu.Frontend.Blazor.Presentation` に置き、Domainだけを参照する通常の.NETライブラリにしています。`ITimeRecordService` を通じて確定した作業記録を受け取り、HTMLやHTTPを準備せずにテストできます。ClientのRazorは表示と操作の接続を担当します。

手書きの通信用DTOとAPI呼び出し処理（作業記録と正常性確認）は `Kizamu.Frontend.Blazor.Infrastructure` に置きます。作業記録の応答DTOを共有Domainへ変換し、通信形式を画面状態へ持ち込みません。通信用DTOは将来生成物へ置き換える対象です。バックエンドのResponse型は参照しません。

依存方向は `Client → Presentation / Infrastructure`、`Infrastructure → Presentation / Domain`、`Presentation → Domain` です。PresentationとInfrastructureはClientを参照しません。プロジェクト参照でコンパイル範囲を分け、既存テスト基盤によるアセンブリ参照の検査で逆方向への依存とPresentationからのHTTP・JSON・ASP.NET Core参照を検出します。この検査はコンパイル結果に残る参照が対象であり、未使用のプロジェクト参照や動的ロードまでを禁止するものではありません。

同じ業務ロジックが必要な場合、ASP.NET CoreとBlazorは.NET版、NodeとReactはTypeScript版のDomainを共用できます。異なる言語間で同じライブラリの直接参照は前提にせず、HTTP API契約で相互運用します。Domainが通信用DTOへ依存する構成にはしません。

フロントエンドはブラウザーだけを意味しません。BlazorのServerは静的ファイル配信と `/api/...` の中継を担当し、記録処理は持ちません。業務APIへのプロジェクト参照はせず、接続先URLだけを設定します。SSR・Autoは採用しておらず、UIは引き続きWebAssemblyとしてブラウザー内で実行します。React公開時にもフロント側の配信・API中継サーバーを置く方針ですが、未実装です。開発時は既存のVite proxyが中継します。

作業記録の開始・終了APIは、`Sec-Fetch-Site: same-origin` の要求だけを受け付けます。ASP.NET CoreとNodeのHTTP境界で検証し、情報の欠落や他の値では、記録を変更する前に `403 Forbidden` を返します。一覧とhealthのGETには、この制約を設けません。対応ブラウザーが標準情報を自動付与するため、BlazorやReactで独自ヘッダーを追加する必要はありません。

公開環境はHTTPS、ローカル開発は `http://localhost` を使用し、Fetch Metadataに対応する現行の主要ブラウザーを対象とします。中継サーバーは元の `Sec-Fetch-Site` を追加・書き換えずに転送してください。画面の公開URLを業務APIへ登録する設定は不要です。この検証は別サイトからのブラウザー経由の更新を拒否するものであり、利用者認証やサーバー間認証の代わりにはなりません。判断理由は [D010](docs/decisions.md#d010画面用の更新apiはfetch-metadataで同一オリジンを検証する) を参照してください。

Visual Studio用の共有一括起動設定は `Kizamu.slnLaunch` と `Kizamu.Blazor.slnLaunch` に置きます。全実装用の `Kizamu.slnx` では `Blazor + ASP.NET Core`、`React + ASP.NET Core`、`Blazor + Node`、`React + Node` の四つを定義しています。業務APIとフロント側サーバーを別プロセスで起動します。プロファイルや公開処理のためにソースを複製しません。IDEでの検出・起動操作はCLIとは別に確認が必要です。

## 実装とリリースの方針

1. 既存の時間記録機能はBlazorへ移行済みです。ASP.NET Coreとの組み合わせを第一実装としてMVPを進めます。
2. .NETの業務APIと画面配信を分離し、Node版に現在の正常性確認・記録の開始・終了・一覧APIを追加しました。Reactの追加機能開発は引き続き保留し、NodeやReactが将来のMVPと同じ機能を持つとは扱いません。
3. 業務APIとフロントサーバーを独立した起動・配備単位にします。ブラウザーはフロントサーバーだけに接続し、API通信はそこから業務APIへ中継します。

当初は製品として一つのリリースバージョンを使い、含まれる実装の機能範囲と検証済みの組み合わせを明示する方針です。全組み合わせを毎回配布する前提にはしません。HTTP APIの互換性と保存データの互換性は別に設計し、別サーバーに同じデータベース構造を強制しません。

ZIPやインストーラーなどの配布形態は未確定です。ASP.NET CoreとBlazorはDockerイメージとしてビルドできます。

標準の `dotnet publish` で業務APIとBlazorサーバーを別々の出力先へ公開します。Blazorサーバーの出力にはClientの静的ファイルが含まれます。以下は動作確認用のフレームワーク依存の出力であり、利用環境にはASP.NET Core Runtime 10が必要です。

```powershell
dotnet publish src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api -c Release -o .artifacts/api-publish
dotnet publish src/frontends/blazor/Kizamu.Frontend.Blazor.Server -c Release -o .artifacts/blazor-server-publish

# 一つ目のターミナル
Push-Location .artifacts/api-publish
dotnet Kizamu.Backend.AspNetCore.Api.dll --urls http://localhost:5213
Pop-Location

# 別のターミナル
Push-Location .artifacts/blazor-server-publish
dotnet Kizamu.Frontend.Blazor.Server.dll --urls http://localhost:5214
Pop-Location
```

サーバーを停止してから `Pop-Location` を実行します。公開時のコード縮小に対応するため、BlazorのJSON読み取りは[.NET標準のソース生成](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation)による型情報を使います。

## Dockerでビルド・起動

WindowsではPowerShell 7とWSL 2を使います。UbuntuとDocker Engine・Composeのセットアップは、リポジトリルートから次を実行します。Ubuntuが未導入の場合はインストール後に初期設定を済ませ、再実行します。WSL自体が未導入の場合は先にWindowsでWSLをインストールしてください。Dockerの導入時はUbuntuのrootとしてパッケージをインストールします。

```powershell
./scripts/Setup-Wsl.ps1
```

イメージをビルドします。WindowsではWSLのUbuntu内でrootとしてDockerを実行します。Linuxでは実行ユーザーが利用できるDocker EngineとComposeを使います。

```powershell
./scripts/Build-Docker.ps1
```

起動と停止にはComposeを使います。WindowsのPowerShellでは次のように実行します。

```powershell
$composeFile = wsl -d Ubuntu -- wslpath -a (Resolve-Path ./compose.yaml).Path
wsl -d Ubuntu -u root -- docker compose -f $composeFile up -d
# 停止時
wsl -d Ubuntu -u root -- docker compose -f $composeFile down
```

`http://localhost:5214` で画面を開きます。画面サーバーがAPIへ中継し、APIのポートはホストへ公開しません。記録はAPIのメモリに保持され、APIの再起動やコンテナの削除で消えます。別名のUbuntuを使用する場合は、セットアップとビルドに `-Distribution` を指定し、上記コマンドの `Ubuntu` もその名前に変更します。

## 必要な環境

- .NET SDK 11.0.100 以降の .NET 11 SDK
- Node.js 24.18.0（Node・React版の確認時）
- npm 11.16.0（Node・React版の確認時）

BlazorとASP.NET Coreだけを開発・起動する場合はNode.jsとnpmは不要です。

標準のIDEはVisual Studio 2026 18.12以降（ASP.NETとWeb開発ワークロード）です。VS Codeでは `Kizamu.code-workspace` を開くとリポジトリ全体を扱えます。C#拡張向けに既定のソリューションを `Kizamu.slnx` に指定しています。IDE上の起動・テスト操作は今回のCLI検証とは別に確認が必要です。VS Code専用の起動・デバッグ設定はまだ用意していません。

## Googleログインのローカル設定

別マシンへクローンしても、ローカルのクライアントID設定は復元されません。必要な環境を準備した後、使う画面と業務APIに同じWebクライアントIDを設定します。以下のパスとコマンドはリポジトリルートを基準にしています。

### Google側の設定とクライアントIDの入手

既存の開発用WebクライアントIDをプロジェクト管理者から受け取るか、Google Cloudの対象プロジェクトの「Google Auth Platform → クライアント」で確認します。新しく用意する場合は、[Google公式の設定手順](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid)に従って「ウェブ アプリケーション」用のクライアントIDと同意画面を設定します。同じ開発用IDを使う場合、マシンごとの再発行は不要です。

Google側の「承認済みのJavaScript生成元」には、このREADMEの起動URLに対応する以下の値を登録します。

- `http://localhost`
- `http://localhost:5214`（Blazorを使う場合）
- `http://localhost:5173`（Reactを使う場合）

ポートやホスト名を変えた場合は、その画面URLの生成元も登録してください。ブラウザーで開く画面の生成元を指定し、業務APIのポート5213はこの一覧へ追加する必要はありません。現在の実装はJavaScriptコールバックで結果を受け取るため、認証結果を受信するリダイレクトURLの追加は不要です。[Google公式の生成元・リダイレクト設定](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid#get_your_google_api_client_id)

以下の `YOUR_GOOGLE_CLIENT_ID` は、入手したクライアントID全体に置き換えます。クライアントシークレットは使用しません。画面のクライアントIDはブラウザーへ配信されるため、これらのファイルに秘密情報を置かないでください。

### 画面側の設定

使う画面の設定ファイルを作成します。両方の画面を試す場合は両方を作成します。

Blazorは `src/frontends/blazor/Kizamu.Frontend.Blazor.Client/wwwroot/appsettings.json` を次の内容で作成します。

```json
{
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID"
  }
}
```

Reactは `src/frontends/react/.env.local` を次の内容で作成します。

```dotenv
VITE_GOOGLE_CLIENT_ID=YOUR_GOOGLE_CLIENT_ID
```

Reactの設定はViteの起動時またはビルド時に読み込まれます。変更後は開発サーバーを再起動し、配布する場合はビルドし直します。[Viteの環境変数の説明](https://vite.dev/guide/env-and-mode)

### 業務API側の設定と起動

ASP.NET Core版・Node版とも、APIを起動するPowerShellで次を実行します。

```powershell
$env:GOOGLE_CLIENT_ID = 'YOUR_GOOGLE_CLIENT_ID'
```

そのターミナルで、下記の各セットアップ手順にあるAPI起動コマンドを実行します。画面側とAPI側に同じIDを指定してください。Reactの `.env.local` は業務APIには読み込まれません。

この環境変数は現在のターミナルとそこから起動するプロセスだけに有効です。新しいターミナルでは再設定します。一括起動用の `scripts/Check-Blazor.ps1` または `scripts/Check-React.ps1` を使う場合も、スクリプトを実行するターミナルで先に設定します。設定を変えた場合はAPIを再起動してください。

Visual Studioで起動する場合は、Windowsの「環境変数」画面で、ユーザー環境変数に名前 `GOOGLE_CLIENT_ID`、値に同じクライアントIDを設定し、Visual Studioを再起動してから共有起動プロファイルを使います。これはターミナルの一時設定と異なり、次回以降もそのマシンのユーザー環境に残ります。IDE経由でのGoogleログインの実機確認は未実施です。

### Gitの除外設定と動作確認

上記のBlazor設定ファイルとReactの `.env.local` はGitの追跡対象から除外しています。作成後、次のコマンドで各ファイルに対応する除外ルールが表示されることを確認できます。

```powershell
git check-ignore -v -- src/frontends/blazor/Kizamu.Frontend.Blazor.Client/wwwroot/appsettings.json
git check-ignore -v -- src/frontends/react/.env.local
```

下記の起動手順で画面を開き、Googleログイン、記録の開始・終了、ログアウトを確認します。設定内容とファイル形式は実装・公式資料と照合しています。実Googleログインの確認範囲は冒頭に記載したローカルの組み合わせに限り、別マシンへのクローンからのセットアップ、IDE起動、公開環境での一連の操作は未検証です。

## Blazor版のセットアップと起動

リポジトリルートから動作確認用スクリプトをPowerShell 7以降で実行すると、ASP.NET CoreとBlazorをビルドして起動し、ブラウザーを開きます。ポート5213または5214が使用中の場合は中断します。

```powershell
./scripts/Check-Blazor.ps1
```

確認後にEnterを押すとサーバーが停止し、メモリ上の記録は失われます。

以下のコマンドはリポジトリルートから実行します。

```powershell
dotnet restore Kizamu.Blazor.slnx

# 一つ目のターミナル
dotnet run --project src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api --launch-profile http

# 別のターミナル
dotnet run --project src/frontends/blazor/Kizamu.Frontend.Blazor.Server --launch-profile http
```

`http://localhost:5214` を開きます。Blazorサーバーが画面を配信し、`/api/...` を業務APIの `http://localhost:5213` へ中継します。UIはブラウザー内で実行されます。二つのプロセスはそれぞれ停止・再起動できます。記録を保持しているのは業務APIなので、その再起動では記録が失われます。

中継先はBlazor Serverの `appsettings.json` にある `ReverseProxy:Clusters:backend:Destinations:backend:Address` です。公開先や別バックエンドへ切り替える場合は、同じキーを環境変数 `ReverseProxy__Clusters__backend__Destinations__backend__Address` 等で上書きできます。ブラウザー側のAPI呼び出しコードや業務APIのビルドには影響しません。

Visual Studioでは `Kizamu.Blazor.slnx` または `Kizamu.slnx` を開き、共有起動プロファイル `Blazor + ASP.NET Core` を選びます。表示されない場合はソリューションの「スタートアッププロジェクトの構成」でAPIとBlazor Serverを両方「開始」にします。複数プロジェクト起動プロファイル機能の有効化が必要な場合があります。IDE上の検出・操作はCLI検証とは別に確認してください。

## 業務APIだけの起動

以下のコマンドはリポジトリルートから実行します。画面配信やフロントエンドの起動を伴わず、HTTP APIだけを確認できます。

```powershell
dotnet restore Kizamu.AspNetCore.slnx
dotnet run --project src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api --no-launch-profile --urls http://localhost:5213
```

正常性確認は `http://localhost:5213/api/health` です。記録一覧のGET、開始と終了のPOSTには、GoogleのIDトークンを `Authorization: Bearer <IDトークン>` で送ります。開始と終了のPOSTでは `Sec-Fetch-Site: same-origin` も必要です。記録一覧のURLは `http://localhost:5213/api/time-records` です。

## Node版業務APIのセットアップと起動

リポジトリルートで実行します。

```powershell
npm ci
npm run dev --workspace=kizamu-backend-node
```

TypeScriptをビルドして `http://localhost:5213` でAPIを起動します。変更後は停止・再実行します。画面は配信しません。ASP.NET Core版と同じポートを使うため、両バックエンドを同時に起動しないでください。

別のターミナルで、Blazor ServerまたはReactのViteを上記・下記の手順で起動します。既存の `/api` 中継先を変更せず、そのままNodeへ接続できます。Visual Studioでは `Kizamu.slnx` の `Blazor + Node` または `React + Node` を選びます。Nodeのデバッグ設定は製品ディレクトリの `.vscode/launch.json` に置き、ビルド済みの `dist/server.js` を起動します（[Visual StudioのNodeデバッグ設定](https://learn.microsoft.com/en-us/visualstudio/javascript/tutorial-nodejs?view=visualstudio#examine-the-launchjson-file)）。

ビルドと起動を分ける場合は次のとおりです。

```powershell
npm run build --workspace=kizamu-backend-node
npm start --workspace=kizamu-backend-node
```

ポートは環境変数 `PORT` で変更できます。その場合はフロント側のAPI中継先も合わせます。Nodeの起動に.NETは不要ですが、Blazor Serverと組み合わせる場合は.NETも必要です。配布用成果物の組み立てはまだ用意していません。

互換性の範囲は、既存画面から行う正常性確認・開始・終了・一覧と、[NodeのHTTPテスト](tests/backends/node/)で確認しています。認証済みの利用者が、同一オリジンから存在しない記録や他の利用者の記録の終了を要求すると、両サーバーはHTTP 404を返します。

## 現行React版のセットアップと起動

保留中のReact版を確認する場合の手順です。以下のコマンドはリポジトリルートから実行します。

```powershell
dotnet restore Kizamu.slnx
npm ci
```

Visual Studioでは `Kizamu.slnx` を開き、共有起動プロファイル `React + ASP.NET Core` でAPIとReactを起動します。業務APIがViteを自動起動する以前のSPA proxy連携は廃止し、起動する組み合わせはIDE側で管理します。APIは `http://localhost:5213`、Viteは `http://localhost:5173` で、Viteの `/api` proxyが業務APIへ転送します。Blazorとのプロファイル切替時には、同じAPIプロセスを二重に起動しないでください。

確認用に、依存関係の準備後にAPIとReactを起動するスクリプトを用意しています。ブラウザーを開き、確認後にEnterを押すと終了します。記録はメモリ上に保持され、終了時に失われます。ポート5213または5173の使用中は起動しません。

```powershell
.\scripts\Check-React.ps1
```

コマンドラインでは二つのターミナルで起動し、`http://localhost:5173` を開きます。

```powershell
dotnet run --project src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api --launch-profile http

# 別のターミナル
npm run dev
```

## ビルドと検証

Blazor、バックエンド、Domainのビルドとテスト:

.NETテストはxUnit.net v3を使い、既存のVSTest経由で実行します。テストプロジェクトだけを実行可能形式に変更しており、製品側のプロジェクト構成は変えていません。Microsoft Testing Platformへの切替は行っていません。

```powershell
dotnet build Kizamu.Blazor.slnx --no-restore
dotnet test Kizamu.Blazor.slnx --no-build --no-restore
```

記録画面の状態・操作と通信境界は、Blazorの画面テストと別の通常.NETテストプロジェクトでも確認できます。Presentation.TestsはBlazor・HTTPテスト基盤を参照せず、Infrastructure.TestsはHTTP要求と応答変換を確認します。どちらもbUnitやASP.NET Coreのテストホストは使用しません。

```powershell
dotnet test tests/frontends/blazor/Kizamu.Frontend.Blazor.Presentation.Tests --no-restore
dotnet test tests/frontends/blazor/Kizamu.Frontend.Blazor.Infrastructure.Tests --no-restore
```

バックエンドとDomainのテストだけを実行する場合（時刻例・開始/終了の状態更新・一覧投影はApplication.Tests、保存順はInfrastructure.Testsで確認し、Api.TestsにはHTTP固有の仕様と開始・終了・一覧の代表的な接続確認を残しています）:

```powershell
dotnet build Kizamu.AspNetCore.slnx --no-restore
dotnet test Kizamu.AspNetCore.slnx --no-build --no-restore
```

構成整理などでReactに影響する変更を行った場合は、Reactも確認します。保留中でも既存機能を壊さないための検証は行います。

```powershell
npm run lint
npm test
npm run build
```

ルートのnpmコマンドは、登録されたworkspacesのうち対応するscriptを持つパッケージへ処理を委譲します。`npm run build` では共有DomainのJavaScript・型宣言、Reactの公開用ビルド、NodeのJavaScript出力、各テストコードの型検査を行います。個別実行には `npm test --workspace=kizamu-domain-typescript-tests`、`npm test --workspace=kizamu-frontend-react-tests`、`npm test --workspace=kizamu-backend-node-tests` を使えます。

共有モデルの開始・終了と期間計算、Domainの依存境界は `tests/domains/typescript/` で確認します。Reactの状態遷移と開始・終了操作、正常性確認、時刻・作業時間の表示用値とタイマー寿命は `tests/frontends/react/presentation/` でHTMLやHTTPを準備せずに確認します。HTTP経路・メソッドとDTO変換は `infrastructure/`、Reactの依存方向のLint設定は `architecture/` で確認します。画面テストには日本語表示・HTML属性と、一覧取得・開始・終了の代表的な接続確認を残しています。

Visual Studioでは `kizamu-domain-typescript-tests.esproj`、`kizamu-frontend-react-tests.esproj`、`kizamu-backend-node-tests.esproj` をテスト用プロジェクトとして追加しています。テストエクスプローラーでの検出・実行はIDE上で確認してください。`dotnet test` のCLI検証は.NETテストだけであり、TypeScriptの共有Domain・React・Nodeを確認するには `npm test` も実行します。

Visual Studio向けの現行ソリューションをCLIで確認する場合は、次のコマンドを使います。Reactの検証は上記npmコマンドと併せて行います。

```powershell
dotnet build Kizamu.slnx --no-restore
dotnet test Kizamu.slnx --no-build --no-restore
```

## ライセンス

Kizamu本体は [MIT License](LICENSE) で公開します。依存ライブラリには、それぞれのライセンス条件が適用されます。

BlazorサーバーのAPI中継には、Microsoft提供の [Yarp.ReverseProxy](https://www.nuget.org/packages/Yarp.ReverseProxy/2.3.0)（MIT）を使用します。

.NETテストでは、コミュニティによるオープンソースのフォーク [AwesomeAssertions](https://awesomeassertions.org/introduction)（Apache-2.0）を使用します。Blazor、Server、Application、Infrastructure、Domainの各テストで使用しており、FluentAssertionsへの依存はありません。

## 未実装

サーバーでのプロジェクト情報と記録の関連付け、記録の修正、作業ツリー、時間配分、Clockify連携、永続化、集計、公開用成果物の組み立ては未実装です。直近の作業状況は実装計画の「現在の次作業」を参照してください。

AsData連携およびWord・Excel・PDFの入出力は今後の検討対象です。今回のBlazor移行には含めず、対応形式、処理場所、依存ライブラリを決めてから別の作業単位として扱います。

Excelには既存のSpreadsheetAsData、Word（DOCX）には既存のDocumentAsDataを連携候補として確認します。DocumentAsDataは名前付きのMERGEFIELDやContent Controlなどをデータとして読み書きするライブラリであり、自由文書の推測抽出、PDF化、レイアウト計算は行いません。PDF入出力は別途検討が必要です。これらの依存関係はまだ導入しておらず、Blazor上での実行も未検証です。
