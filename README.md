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

サーバーとクライアントの実装を、単一リポジトリ内で技術ごとに分けます。現在存在する構成は次のとおりです。

```text
kizamu/
├─ Kizamu.slnx                      現行ASP.NET Core・React版のソリューション
├─ servers/
│  └─ aspnetcore/
│     ├─ Kizamu.AspNetCore.slnx      サーバーだけのソリューション
│     ├─ src/
│     │  ├─ Kizamu.Server/
│     │  ├─ Kizamu.Application/
│     │  ├─ Kizamu.Domain/
│     │  └─ Kizamu.Infrastructure/
│     └─ tests/
│        ├─ Kizamu.Server.Tests/
│        ├─ Kizamu.Application.Tests/
│        ├─ Kizamu.Domain.Tests/
│        └─ Kizamu.Infrastructure.Tests/
├─ clients/
│  └─ react/                        既存React実装・追加開発は保留
└─ docs/                           製品仕様・作業方針
```

Visual Studioのソリューション表示は、物理配置や公開時の組み合わせとは別に扱います。ルートのソリューションは現行版の確認用に残し、サーバーだけのビルドとテストには `servers/aspnetcore/Kizamu.AspNetCore.slnx` を使います。Reactは自身のnpmコマンドでビルド、テストできます。

日常の開発と差分レビューはVisual Studioを基本とし、ReactやNodeを含め、技術や組み合わせによってIDEを使い分けることは前提にしません。一方で、VS Codeでの作業も考慮します。ビルドとテストの本体は `dotnet` と `npm` のコマンドに置き、IDE固有の設定はその操作の補助として扱います。

将来のクライアントは `clients/blazor/`、別サーバーは `servers/node/` などへ追加しますが、未着手の実装のために空プロジェクトは作りません。技術が増えても、実装間でソースコードやサーバー内部のDomainモデルを共有せず、言語非依存のHTTP API契約で互換性を扱います。共通契約の文書やテストは、別実装を導入する時点で整備します。

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

標準のIDEはVisual Studio 2026（ASP.NETとWeb開発ワークロード）です。VS Codeを使う場合も、リポジトリルートまたは対象実装のフォルダーを開き、以下のCLI手順で起動、ビルド、テストできます。VS Code専用の起動・デバッグ設定はまだ用意していません。

## サーバーだけのセットアップと起動

以下のコマンドはリポジトリルートから実行します。ReactやViteを起動せず、HTTP APIだけを確認できます。

```powershell
dotnet restore servers/aspnetcore/Kizamu.AspNetCore.slnx
dotnet run --project servers/aspnetcore/src/Kizamu.Server --no-launch-profile --urls http://localhost:5213
```

正常性確認は `http://localhost:5213/api/health`、記録一覧は `http://localhost:5213/api/time-records` です。この起動方法は画面を配信しません。

## 現行React版のセットアップと起動

保留中のReact版を確認する場合の手順です。以下のコマンドはリポジトリルートから実行します。

```powershell
dotnet restore Kizamu.slnx
npm ci --prefix clients/react
```

Visual Studioでは `Kizamu.slnx` を開き、`Kizamu.Server` をスタートアッププロジェクトとしてデバッグ開始します。ASP.NET CoreのSPA proxyがViteを自動起動し、ブラウザをReact画面へ転送するため、別のターミナルでReactを起動する必要はありません。サーバーは `http://localhost:5213`、Viteは `http://localhost:5173` で起動し、Viteの `/api` proxyがサーバーへ転送します。

コマンドラインでも、ASP.NET Coreを起動して `http://localhost:5213` をブラウザで開くとViteが自動起動します。

```powershell
dotnet run --project servers/aspnetcore/src/Kizamu.Server --launch-profile http
```

## ビルドと検証

```powershell
dotnet build servers/aspnetcore/Kizamu.AspNetCore.slnx --no-restore
dotnet test servers/aspnetcore/Kizamu.AspNetCore.slnx --no-build
```

構成整理などでReactに影響する変更を行った場合は、Reactも確認します。保留中でも既存機能を壊さないための検証は行います。

```powershell
npm run lint --prefix clients/react
npm test --prefix clients/react
npm run build --prefix clients/react
```

Visual Studio向けの現行ソリューション全体を確認する場合は、次のコマンドを使います。

```powershell
dotnet build Kizamu.slnx --no-restore
dotnet test Kizamu.slnx --no-build
```

## ライセンス

Kizamu本体は [MIT License](LICENSE) で公開します。依存ライブラリには、それぞれのライセンス条件が適用されます。

サーバーテストだけで使用するFluentAssertions 8系は、オープンソースおよび非商用利用では無料ですが、商用利用には有償ライセンスが必要です。フォーク、クローン、再公開後に商用利用する場合は、[FluentAssertions公式のライセンス案内](https://fluentassertions.com/introduction)で利用時点の条件を確認してください。

## 未実装

Blazorクライアント、プロジェクト登録・タブ切替、プロジェクト単位の複数同時記録、記録の修正、作業ツリー、時間配分、Clockify連携、ログイン、ユーザー管理、永続化、集計、公開用成果物の組み立ては未実装です。次は既存機能のBlazorへの移行を、小さな単位で進めます。
