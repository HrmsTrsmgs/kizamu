# Kizamu

作業を中断せず、プロジェクトごとの時間を素早く記録するタイムトラッカー。

現在はASP.NET CoreとReactによる開発環境の足場だけを提供しています。画面には準備完了とサーバー接続状態を表示します。記録を管理から分離し、未分類のまま素早く記録できる製品構想の詳細は [docs/concept.md](docs/concept.md)、MVPまでの暫定的な実装順序は [docs/roadmap.md](docs/roadmap.md)、具体的な作業計画は [docs/mvp-implementation-plan.md](docs/mvp-implementation-plan.md) を参照してください。

## 技術構成

- .NET 10 LTS / ASP.NET Core Minimal API / xUnit / FluentAssertions
- React / TypeScript（strict）/ Vite
- Vitest / React Testing Library / ESLint / npm

```text
Kizamu.slnx
├─ src/Kizamu.Server
├─ src/Kizamu.Client
└─ tests/Kizamu.Server.Tests
```

## 必要な環境

- .NET SDK 10.0.302 以降の .NET 10 SDK
- Node.js 24.18.0
- npm 11.16.0
- Visual Studio 2026（ASP.NETとWeb開発ワークロード）

## セットアップと起動

```powershell
dotnet restore Kizamu.slnx
Set-Location src/Kizamu.Client
npm ci
```

Visual Studioでは `Kizamu.slnx` を開き、`Kizamu.Server` をスタートアッププロジェクトとしてデバッグ開始します。ASP.NET CoreのSPA proxyがViteを自動起動し、ブラウザをReact画面へ転送するため、別のターミナルでReactを起動する必要はありません。サーバーは `http://localhost:5213`、Viteは `http://localhost:5173` で起動し、Viteの `/api` proxyがサーバーへ転送します。

コマンドラインでも、ASP.NET Coreを起動して `http://localhost:5213` をブラウザで開くとViteが自動起動します。

```powershell
dotnet run --project src/Kizamu.Server --launch-profile http
```

## ビルドと検証

```powershell
dotnet build Kizamu.slnx --no-restore
dotnet test Kizamu.slnx --no-build
npm run lint --prefix src/Kizamu.Client
npm test --prefix src/Kizamu.Client
npm run build --prefix src/Kizamu.Client
```

## ライセンス

Kizamu本体は [MIT License](LICENSE) で公開します。依存ライブラリには、それぞれのライセンス条件が適用されます。

サーバーテストだけで使用するFluentAssertions 8系は、オープンソースおよび非商用利用では無料ですが、商用利用には有償ライセンスが必要です。フォーク、クローン、再公開後に商用利用する場合は、[FluentAssertions公式のライセンス案内](https://fluentassertions.com/introduction)で利用時点の条件を確認してください。

## 未実装

時間記録、プロジェクト、作業ツリー、時間配分、Clockify連携、ログイン、ユーザー管理、永続化、集計は未実装です。次の実装候補は時間記録と決め打ちせず、まず小さな単位で仕様を確定してから進めます。
