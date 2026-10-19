# Kizamu

作業を中断せず、プロジェクトごとの時間を素早く記録するタイムトラッカー。

現在はASP.NET CoreとBlazor WebAssembly、および既存React版で、プロジェクト未指定の時間記録の開始、終了、一覧、開始・終了時刻、作業時間の表示を実装しています。記録はサーバーのメモリ上だけに保持し、サーバーを再起動すると失われます。

MVPの第一実装はASP.NET CoreとBlazor WebAssemblyです。既存の時間記録機能をBlazorへ移し、Reactは現状を保持して追加開発を保留します。

製品構想は [docs/concept.md](docs/concept.md)、MVPまでの暫定的な順序は [docs/roadmap.md](docs/roadmap.md)、具体的な作業計画は [docs/mvp-implementation-plan.md](docs/mvp-implementation-plan.md) を参照してください。

開発を再開するときは、[現在の次作業](docs/mvp-implementation-plan.md#現在の次作業)、[判断の理由と記録先](docs/decisions.md)、[未確定の開発候補](docs/development-candidates.md) から確認できます。AWSのAI-DLCから、必要な仕様を対話で引き出し、合意と根拠を残して次回へ引き継ぐ考え方を取り入れています。日々の実装は既存のTDD手順に従います。

## 技術構成

- .NET 10 LTS / ASP.NET Core Minimal API / Blazor WebAssembly
- xUnit / bUnit / AwesomeAssertions
- React / TypeScript（strict）/ Vite
- Vitest / React Testing Library / ESLint / npm

## リポジトリ構成

単一リポジトリで、最上位の `src/` と `tests/` を分け、その中を役割と実装技術で整理します。現在存在する構成は次のとおりです。

```text
kizamu/
├─ src/
│  ├─ backends/
│  │  └─ aspnetcore/
│  │     ├─ Kizamu.Backend.AspNetCore.Api/
│  │     ├─ Kizamu.Backend.AspNetCore.Application/
│  │     └─ Kizamu.Backend.AspNetCore.Infrastructure/
│  ├─ frontends/
│  │  ├─ blazor/
│  │  │  └─ Kizamu.Frontend.Blazor.Client/   第一実装・WebAssembly UI
│  │  └─ react/                     既存React実装・追加開発は保留
│  │     ├─ kizamu-frontend-react.esproj
│  │     ├─ package.json
│  │     └─ src/
│  └─ domains/
│     └─ dotnet/
│        └─ Kizamu.Domain/
├─ tests/
│  ├─ backends/
│  │  └─ aspnetcore/
│  │     ├─ Kizamu.Backend.AspNetCore.Api.Tests/
│  │     ├─ Kizamu.Backend.AspNetCore.Application.Tests/
│  │     └─ Kizamu.Backend.AspNetCore.Infrastructure.Tests/
│  ├─ frontends/
│  │  ├─ blazor/
│  │  │  └─ Kizamu.Frontend.Blazor.Client.Tests/
│  │  └─ react/
│  │     ├─ kizamu-frontend-react-tests.esproj
│  │     ├─ package.json
│  │     ├─ vitest.config.ts
│  │     └─ …                       既存テストと補助コード
│  └─ domains/
│     └─ dotnet/
│        └─ Kizamu.Domain.Tests/
├─ docs/                            製品仕様・作業方針
├─ Kizamu.slnx                      全実装の作業用
├─ Kizamu.Blazor.slnx               BlazorとASP.NET Core、Domain、そのテスト
├─ Kizamu.AspNetCore.slnx            ASP.NET CoreとDomain、そのテスト
├─ Kizamu.code-workspace            VS Code用の作業設定
├─ package.json                     npm workspacesと共通コマンド
└─ package-lock.json                npm依存関係の共通lockfile
```

Visual Studioのソリューション表示は、物理配置や公開時の組み合わせとは別に扱います。同じ実装のプロジェクトとテストを、`Backends/ASP.NET Core`、`Frontends/Blazor`、`Frontends/React`、`Domains/.NET` の論理フォルダーへまとめています。Blazor開発には `Kizamu.Blazor.slnx`、バックエンドとDomainのテストだけの実行には `Kizamu.AspNetCore.slnx` を使います。APIプロジェクトはBlazorの静的ファイルを配信するためClientをプロジェクト参照しており、後者のビルドでもClientは参照先としてビルドされます。

.NETの実装固有プロジェクトは `Kizamu.<所属>.<技術>.<役割>` と命名します。業務バックエンドのHTTP APIは `Backend.AspNetCore.Api`、Blazorのブラウザー側UIは `Frontend.Blazor.Client`、将来のUIサーバーは `Frontend.Blazor.Server` とし、ClientとServerは同じUI実装内の対になる名前です。Domainと通信契約は実装技術に所属させず、それぞれ `Kizamu.Domain` と将来の `Kizamu.Api.Contracts` として区別します。

npmパッケージは各技術の習慣に合わせて小文字のハイフン区切りにし、現在のReact版は `kizamu-frontend-react` と命名します。テストは.NETで `.Tests`、npmで `-tests` を付けます。Reactの `.esproj` もnpmパッケージ名に揃えます。

Domainはバックエンド専用の位置から分離し、ASP.NET CoreとBlazorから共用しています。Blazorの画面は通信DTOではなくDomainの作業記録を保持します。配置の共通化によって、すべてのフロントエンドへ同じ内部モデルの利用を強制するものではありません。

Reactの製品パッケージとテストパッケージは独立させ、ルートのnpm workspacesで管理します。依存関係のインストールとlockfileの更新はリポジトリルートで行います。各 `.esproj` による個別の自動 `npm install` は無効にしており、最初にルートで `npm ci` を実行する必要があります。

日常の開発と差分レビューはVisual Studioを基本とし、ReactやNodeを含め、技術や組み合わせによってIDEを使い分けることは前提にしません。一方で、VS Codeでの作業も考慮します。ビルドとテストの本体は `dotnet` と `npm` のコマンドに置き、IDE固有の設定はその操作の補助として扱います。

### 今後追加する配置（未実装）

次の配置は予定です。現在はディレクトリ、生成物、空プロジェクトを作成していません。

| 配置先 | 用途・方針 |
| --- | --- |
| `contracts/openapi.yaml` | 実装言語から独立した共通HTTP API契約。複数バックエンドの互換性の基準にします。 |
| `src/api/dotnet/Kizamu.Api.Contracts/`、`src/api/typescript/` | OpenAPIから生成する通信用DTO等。TypeScriptパッケージは `kizamu-api-typescript`。Domainとは分離します。 |
| `src/frontends/blazor/Kizamu.Frontend.Blazor.Server/` | SSR・Auto等を採用する場合に追加するUIサーバー。WebAssemblyのClientは実装済みです。 |
| `src/backends/node/` | `kizamu-backend-node`。Nodeによる業務APIの別実装。ReactのSSRサーバーとは区別します。 |
| `src/domains/typescript/` | `kizamu-domain-typescript`。NodeやReactから必要に応じて共用する、実行環境に依存しない業務ロジック。 |
| `tests/backends/node/`、`tests/domains/typescript/` | 対応する実装の `-tests` パッケージ。BlazorのClientと配信のテストは導入済みで、UIサーバーを追加する場合は `.Tests` プロジェクトも追加します。 |
| `tests/api/` | 通信コードの検証が必要になった場合のテスト。生成DTOの数だけ形式的にテストを増やしません。 |
| `tests/contracts/` | 各バックエンドが共通のHTTP API契約を満たすことを確認するテスト。 |
| `tests/system/` | フロントエンドとバックエンドを組み合わせた製品全体のテスト。 |
| `eng/` | 必要になったコード生成、ビルド、公開成果物の組み立ての補助手順。 |

通信用DTOは共通のOpenAPI契約から各言語向けに生成する方針です。API呼び出しコードの生成も検討対象とし、対象言語・ランタイムの進化への追随、必要な機能への対応速度、拡張性、実際の更新履歴と生成結果を確認して採否を決めます。生成ツールは未選定です。生成物は手書き部分と区別し、手修正せず、ツールのバージョンと設定を固定して再生成できるようにします。生成DTOと呼び出し処理のプロジェクト分割は、採用ツールの出力と依存関係を確認して決めます。

現在のBlazorの通信用DTOとAPI呼び出し処理はClient内の手書き実装です。API呼び出し処理が応答DTOを共有Domainの作業記録へ変換して画面へ渡し、通信形式を画面の状態へ持ち込まない境界にしています。通信用DTOは将来生成物へ置き換える対象です。ClientはサーバーのResponse型を参照していません。

同じ業務ロジックが必要な場合、ASP.NET CoreとBlazorは.NET版、NodeとReactはTypeScript版のDomainを共用できます。異なる言語間で同じライブラリの直接参照は前提にせず、HTTP API契約で相互運用します。Domainが通信用DTOへ依存する構成にはしません。

フロントエンドはブラウザーだけを意味しません。BlazorのUIサーバーや、将来Reactへ追加するSSRサーバーも、それぞれ `src/frontends/` 配下へ置きます。スタンドアロンBlazor WebAssemblyでは専用のUIサーバーを作りません。SSR・Autoの採用自体は未確定です。

現在はサーバーの起動プロファイルで `blazor`（ASP.NET CoreとBlazor）と `http`（ASP.NET CoreとReact）の2種類を選べます。Node導入後はASP.NET Core/NodeとBlazor/Reactの4種類を選べることを目標とします。プロファイルや公開処理のためにソースを複製しません。

## 実装とリリースの方針

1. 既存の時間記録機能はBlazorへ移行済みです。ASP.NET Coreとの組み合わせを第一実装としてMVPを進めます。
2. Reactの開発再開とNodeなどの別実装はMVP後に検討します。保留中のReactが将来のMVPと同じ機能を持つとは扱いません。
3. 各実装の成果物と、利用者が起動するサーバー・クライアントの組み合わせを区別します。ASP.NET CoreがBlazor WebAssemblyの静的ファイルとAPIを提供し、一つのサーバー起動で利用できる構成にしています。

当初は製品として一つのリリースバージョンを使い、含まれる実装の機能範囲と検証済みの組み合わせを明示する方針です。全組み合わせを毎回配布する前提にはしません。HTTP APIの互換性と保存データの互換性は別に設計し、別サーバーに同じデータベース構造を強制しません。

対応OS、ランタイム同梱、ZIPやインストーラーなどの配布形態と、その組み立て処理は未実装・未確定です。

標準の `dotnet publish` でAPIとBlazorの静的ファイルを同じ出力先へ公開できます。以下は動作確認用のフレームワーク依存の出力であり、利用環境にはASP.NET Core Runtime 10が必要です。

```powershell
dotnet publish src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api -c Release -o .artifacts/blazor-publish
Push-Location .artifacts/blazor-publish
dotnet Kizamu.Backend.AspNetCore.Api.dll --urls http://localhost:5213
Pop-Location
```

サーバーを停止してから `Pop-Location` を実行します。公開時のコード縮小に対応するため、BlazorのJSON読み取りは[.NET標準のソース生成](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation)による型情報を使います。

## 必要な環境

- .NET SDK 10.0.302 以降の .NET 10 SDK
- Node.js 24.18.0（現行React版の確認時）
- npm 11.16.0（現行React版の確認時）

Blazor版だけを開発・起動する場合はNode.jsとnpmは不要です。

標準のIDEはVisual Studio 2026（ASP.NETとWeb開発ワークロード）です。VS Codeでは `Kizamu.code-workspace` を開くとリポジトリ全体を扱えます。C#拡張向けに既定のソリューションを `Kizamu.slnx` に指定しています。IDE上の起動・テスト操作は今回のCLI検証とは別に確認が必要です。VS Code専用の起動・デバッグ設定はまだ用意していません。

## Blazor版のセットアップと起動

以下のコマンドはリポジトリルートから実行します。

```powershell
dotnet restore Kizamu.Blazor.slnx
dotnet run --project src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api --launch-profile blazor
```

`http://localhost:5213` を開きます。APIとBlazor画面を同じASP.NET Coreサーバーが配信するため、別の開発サーバーは起動しません。WebAssemblyでのUI実行はブラウザ内です。

Visual Studioでは `Kizamu.Blazor.slnx` を開き、`Kizamu.Backend.AspNetCore.Api` をスタートアッププロジェクトにして、起動プロファイル `blazor` を選びます。プロファイルとCLI起動は確認済みですが、Visual Studio上の操作は別途確認が必要です。

## Reactを起動しないサーバー起動

以下のコマンドはリポジトリルートから実行します。ReactやViteを起動せず、HTTP APIを確認できます。Blazorの静的ファイルも配信されます。

```powershell
dotnet restore Kizamu.AspNetCore.slnx
dotnet run --project src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api --no-launch-profile --urls http://localhost:5213
```

正常性確認は `http://localhost:5213/api/health`、記録一覧は `http://localhost:5213/api/time-records` です。

## 現行React版のセットアップと起動

保留中のReact版を確認する場合の手順です。以下のコマンドはリポジトリルートから実行します。

```powershell
dotnet restore Kizamu.slnx
npm ci
```

Visual Studioでは `Kizamu.slnx` を開き、`Kizamu.Backend.AspNetCore.Api` をスタートアッププロジェクトにして、起動プロファイル `http` を選んでデバッグ開始します。ASP.NET CoreのSPA proxyがViteを自動起動し、ブラウザをReact画面へ転送するため、別のターミナルでReactを起動する必要はありません。サーバーは `http://localhost:5213`、Viteは `http://localhost:5173` で起動し、Viteの `/api` proxyがサーバーへ転送します。Blazor版と同じAPIポートを使うため、両プロファイルを同時には起動しません。

コマンドラインでも、ASP.NET Coreを起動して `http://localhost:5213` をブラウザで開くとViteが自動起動します。

```powershell
dotnet run --project src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api --launch-profile http
```

## ビルドと検証

Blazor、バックエンド、Domainのビルドとテスト:

```powershell
dotnet build Kizamu.Blazor.slnx --no-restore
dotnet test Kizamu.Blazor.slnx --no-build --no-restore
```

バックエンドとDomainのテストだけを実行する場合:

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

ルートのnpmコマンドは、登録されたworkspacesのうち対応するscriptを持つパッケージへ処理を委譲します。`npm run build` ではReactの公開用ビルドとテストコードの型検査を行います。個別実行には `npm test --workspace=kizamu-frontend-react-tests` などを使えます。

Visual Studioでは `kizamu-frontend-react-tests.esproj` をテスト用プロジェクトとして追加しています。テストエクスプローラーでの検出・実行はIDE上で確認してください。`dotnet test` のCLI検証は.NETテストだけであり、Reactを確認するには `npm test` も実行します。

Visual Studio向けの現行ソリューションをCLIで確認する場合は、次のコマンドを使います。Reactの検証は上記npmコマンドと併せて行います。

```powershell
dotnet build Kizamu.slnx --no-restore
dotnet test Kizamu.slnx --no-build --no-restore
```

## ライセンス

Kizamu本体は [MIT License](LICENSE) で公開します。依存ライブラリには、それぞれのライセンス条件が適用されます。

.NETテストでは、コミュニティによるオープンソースのフォーク [AwesomeAssertions](https://awesomeassertions.org/introduction)（Apache-2.0）を使用します。Blazor、Server、Application、Infrastructure、Domainの各テストで使用しており、FluentAssertionsへの依存はありません。

## 未実装

プロジェクト登録・タブ切替、プロジェクト単位の複数同時記録、記録の修正、作業ツリー、時間配分、Clockify連携、ログイン、ユーザー管理、永続化、集計、公開用成果物の組み立ては未実装です。次はVisual StudioからBlazor版を起動する確認を経て、最小限のプロジェクト登録へ進みます。

AsData連携およびWord・Excel・PDFの入出力は今後の検討対象です。今回のBlazor移行には含めず、対応形式、処理場所、依存ライブラリを決めてから別の作業単位として扱います。

Excelには既存のSpreadsheetAsData、Word（DOCX）には既存のDocumentAsDataを連携候補として確認します。DocumentAsDataは名前付きのMERGEFIELDやContent Controlなどをデータとして読み書きするライブラリであり、自由文書の推測抽出、PDF化、レイアウト計算は行いません。PDF入出力は別途検討が必要です。これらの依存関係はまだ導入しておらず、Blazor上での実行も未検証です。
