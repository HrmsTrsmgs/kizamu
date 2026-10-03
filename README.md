# Kizamu

作業を中断せず、プロジェクトごとの時間を素早く記録するタイムトラッカー。

現在はASP.NET CoreとReactで、プロジェクト未指定の時間記録の開始、終了、一覧、開始・終了時刻、作業時間の表示を実装しています。記録はサーバーのメモリ上だけに保持し、サーバーを再起動すると失われます。

MVPの第一実装はASP.NET CoreとBlazor WebAssemblyへ切り替える方針です。Blazorはまだ未実装で、Reactは現状を保持して追加開発を保留します。

製品構想は [docs/concept.md](docs/concept.md)、MVPまでの暫定的な順序は [docs/roadmap.md](docs/roadmap.md)、具体的な作業計画は [docs/mvp-implementation-plan.md](docs/mvp-implementation-plan.md) を参照してください。

## 技術構成

- .NET 10 LTS / ASP.NET Core Minimal API / xUnit / FluentAssertions
- React / TypeScript（strict）/ Vite
- Vitest / React Testing Library / ESLint / npm

## リポジトリ構成

単一リポジトリで、最上位の `src/` と `tests/` を分け、その中を役割と実装技術で整理します。現在存在する構成は次のとおりです。

```text
kizamu/
├─ src/
│  ├─ backends/
│  │  └─ aspnetcore/
│  │     ├─ Kizamu.Server/
│  │     ├─ Kizamu.Application/
│  │     └─ Kizamu.Infrastructure/
│  ├─ frontends/
│  │  └─ react/                     既存React実装・追加開発は保留
│  │     ├─ Kizamu.Client.esproj
│  │     ├─ package.json
│  │     └─ src/
│  └─ domains/
│     └─ dotnet/
│        └─ Kizamu.Domain/
├─ tests/
│  ├─ backends/
│  │  └─ aspnetcore/
│  │     ├─ Kizamu.Server.Tests/
│  │     ├─ Kizamu.Application.Tests/
│  │     └─ Kizamu.Infrastructure.Tests/
│  ├─ frontends/
│  │  └─ react/
│  │     ├─ Kizamu.Client.Tests.esproj
│  │     ├─ package.json
│  │     ├─ vitest.config.ts
│  │     └─ …                       既存テストと補助コード
│  └─ domains/
│     └─ dotnet/
│        └─ Kizamu.Domain.Tests/
├─ docs/                            製品仕様・作業方針
├─ Kizamu.slnx                      現行ASP.NET Core・React版の作業用
├─ Kizamu.AspNetCore.slnx            ASP.NET CoreとDomain、そのテスト
├─ Kizamu.code-workspace            VS Code用の作業設定
├─ package.json                     npm workspacesと共通コマンド
└─ package-lock.json                npm依存関係の共通lockfile
```

Visual Studioのソリューション表示は、物理配置や公開時の組み合わせとは別に扱います。同じ実装のプロジェクトとテストを、ASP.NET Core、React、Domain/.NETの論理フォルダーへまとめています。サーバーだけのビルドとテストにはルートの `Kizamu.AspNetCore.slnx` を使います。

Domainはバックエンド専用の位置から分離しています。現在の利用側はASP.NET Coreだけですが、ブラウザーで動作する業務ロジックをBlazorから共用できる位置に置きます。配置の共通化によって、すべてのフロントエンドへ同じ内部モデルの利用を強制するものではありません。

Reactの製品パッケージとテストパッケージは独立させ、ルートのnpm workspacesで管理します。依存関係のインストールとlockfileの更新はリポジトリルートで行います。各 `.esproj` による個別の自動 `npm install` は無効にしており、最初にルートで `npm ci` を実行する必要があります。

日常の開発と差分レビューはVisual Studioを基本とし、ReactやNodeを含め、技術や組み合わせによってIDEを使い分けることは前提にしません。一方で、VS Codeでの作業も考慮します。ビルドとテストの本体は `dotnet` と `npm` のコマンドに置き、IDE固有の設定はその操作の補助として扱います。

### 今後追加する配置（未実装）

次の配置は予定です。現在はディレクトリ、生成物、空プロジェクトを作成していません。

| 配置先 | 用途・方針 |
| --- | --- |
| `contracts/openapi.yaml` | 実装言語から独立した共通HTTP API契約。複数バックエンドの互換性の基準にします。 |
| `src/api/dotnet/`、`src/api/typescript/` | OpenAPIから生成する通信用DTO等。Domainとは分離します。 |
| `src/frontends/blazor/` | Blazor UI。WebAssemblyのClientと、SSR・Auto等を採用する場合のUI用Hostを置きます。 |
| `src/backends/node/` | Nodeによる業務APIの別実装。ReactのSSRサーバーとは区別します。 |
| `src/domains/typescript/` | NodeやReactから必要に応じて共用する、実行環境に依存しない業務ロジック。 |
| `tests/frontends/blazor/`、`tests/backends/node/`、`tests/domains/typescript/` | 対応する実装のテスト。BlazorにはUI用Hostのテストも含めます。 |
| `tests/api/` | 通信コードの検証が必要になった場合のテスト。生成DTOの数だけ形式的にテストを増やしません。 |
| `tests/contracts/` | 各バックエンドが共通のHTTP API契約を満たすことを確認するテスト。 |
| `tests/system/` | フロントエンドとバックエンドを組み合わせた製品全体のテスト。 |
| `eng/` | 必要になったコード生成、ビルド、公開成果物の組み立ての補助手順。 |
| `Kizamu.Blazor.slnx` | Blazorと必要な共通部分、そのテストを扱うソリューション。 |

通信用DTOは共通のOpenAPI契約から各言語向けに生成する方針です。API呼び出しコードの生成も検討対象とし、対象言語・ランタイムの進化への追随、必要な機能への対応速度、拡張性、実際の更新履歴と生成結果を確認して採否を決めます。生成ツールは未選定です。生成物は手書き部分と区別し、手修正せず、ツールのバージョンと設定を固定して再生成できるようにします。生成DTOと呼び出し処理のプロジェクト分割は、採用ツールの出力と依存関係を確認して決めます。

同じ業務ロジックが必要な場合、ASP.NET CoreとBlazorは.NET版、NodeとReactはTypeScript版のDomainを共用できます。異なる言語間で同じライブラリの直接参照は前提にせず、HTTP API契約で相互運用します。Domainが通信用DTOへ依存する構成にはしません。

フロントエンドはブラウザーだけを意味しません。BlazorのUI用Hostや、将来Reactへ追加するSSRサーバーも、それぞれ `src/frontends/` 配下へ置きます。スタンドアロンBlazor WebAssemblyではUI用Hostを作りません。SSR・Autoの採用自体は未確定です。

起動プロファイルで組み合わせを選ぶ構成は今後整備します。現在はASP.NET CoreとReactの1種類、Blazor導入後は2種類、Node導入後はASP.NET Core/NodeとBlazor/Reactの4種類を選べることを目標とします。プロファイルや公開処理のためにソースを複製しません。

## 実装とリリースの方針

1. 既存機能を一つずつBlazorへ移し、ASP.NET Coreとの組み合わせを第一実装としてMVPを進めます。
2. Reactの開発再開とNodeなどの別実装はMVP後に検討します。保留中のReactが将来のMVPと同じ機能を持つとは扱いません。
3. 各実装の成果物と、利用者が起動するサーバー・クライアントの組み合わせを区別します。最初の公開版は、ASP.NET CoreがBlazor WebAssemblyの静的ファイルとAPIを提供し、一つのサーバー起動で利用できる構成を目標とします。

当初は製品として一つのリリースバージョンを使い、含まれる実装の機能範囲と検証済みの組み合わせを明示する方針です。全組み合わせを毎回配布する前提にはしません。HTTP APIの互換性と保存データの互換性は別に設計し、別サーバーに同じデータベース構造を強制しません。

公開用の組み立て処理、対応OS、ランタイム同梱、ZIPやインストーラーなどの配布形態は未実装・未確定です。今回の構成整理では追加しません。

## 必要な環境

- .NET SDK 10.0.302 以降の .NET 10 SDK
- Node.js 24.18.0（現行React版の確認時）
- npm 11.16.0（現行React版の確認時）

標準のIDEはVisual Studio 2026（ASP.NETとWeb開発ワークロード）です。VS Codeでは `Kizamu.code-workspace` を開くとリポジトリ全体を扱えます。C#拡張向けに既定のソリューションを `Kizamu.slnx` に指定しています。IDE上の起動・テスト操作は今回のCLI検証とは別に確認が必要です。VS Code専用の起動・デバッグ設定はまだ用意していません。

## サーバーだけのセットアップと起動

以下のコマンドはリポジトリルートから実行します。ReactやViteを起動せず、HTTP APIだけを確認できます。

```powershell
dotnet restore Kizamu.AspNetCore.slnx
dotnet run --project src/backends/aspnetcore/Kizamu.Server --no-launch-profile --urls http://localhost:5213
```

正常性確認は `http://localhost:5213/api/health`、記録一覧は `http://localhost:5213/api/time-records` です。この起動方法は画面を配信しません。

## 現行React版のセットアップと起動

保留中のReact版を確認する場合の手順です。以下のコマンドはリポジトリルートから実行します。

```powershell
dotnet restore Kizamu.slnx
npm ci
```

Visual Studioでは `Kizamu.slnx` を開き、`Kizamu.Server` をスタートアッププロジェクトとしてデバッグ開始します。ASP.NET CoreのSPA proxyがViteを自動起動し、ブラウザをReact画面へ転送するため、別のターミナルでReactを起動する必要はありません。サーバーは `http://localhost:5213`、Viteは `http://localhost:5173` で起動し、Viteの `/api` proxyがサーバーへ転送します。

コマンドラインでも、ASP.NET Coreを起動して `http://localhost:5213` をブラウザで開くとViteが自動起動します。

```powershell
dotnet run --project src/backends/aspnetcore/Kizamu.Server --launch-profile http
```

## ビルドと検証

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

ルートのnpmコマンドは、登録されたworkspacesのうち対応するscriptを持つパッケージへ処理を委譲します。`npm run build` ではReactの公開用ビルドとテストコードの型検査を行います。個別実行には `npm test --workspace=kizamu-client-tests` などを使えます。

Visual Studioでは `Kizamu.Client.Tests.esproj` をテスト用プロジェクトとして追加しています。テストエクスプローラーでの検出・実行はIDE上で確認してください。`dotnet test` のCLI検証は.NETテストだけであり、Reactを確認するには `npm test` も実行します。

Visual Studio向けの現行ソリューションをCLIで確認する場合は、次のコマンドを使います。Reactの検証は上記npmコマンドと併せて行います。

```powershell
dotnet build Kizamu.slnx --no-restore
dotnet test Kizamu.slnx --no-build --no-restore
```

## ライセンス

Kizamu本体は [MIT License](LICENSE) で公開します。依存ライブラリには、それぞれのライセンス条件が適用されます。

サーバーテストだけで使用するFluentAssertions 8系は、オープンソースおよび非商用利用では無料ですが、商用利用には有償ライセンスが必要です。フォーク、クローン、再公開後に商用利用する場合は、[FluentAssertions公式のライセンス案内](https://fluentassertions.com/introduction)で利用時点の条件を確認してください。

## 未実装

Blazorクライアント、プロジェクト登録・タブ切替、プロジェクト単位の複数同時記録、記録の修正、作業ツリー、時間配分、Clockify連携、ログイン、ユーザー管理、永続化、集計、公開用成果物の組み立ては未実装です。次は既存機能のBlazorへの移行を、小さな単位で進めます。
