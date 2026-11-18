# Azure共有デモのCI/CD

現在の4画面・2 APIをGitHub ActionsとBicepで構成する。この定義は共有メモリによるデモを対象にする。mainからGoogleログイン実装を取り込んだが、以下の公開用設定は未対応のため、PRのマージと公開更新は保留している。DBは別作業とする。

## 構成と更新

- PRではnpmのlint・build・testと.NETのbuild・test、Bicepのコンパイルを実行する。
- mainへのpushとmainを選んだ手動実行では、検証成功後にAzureへデプロイする。
- レジストリ、ログ、Container Apps環境、取得用IDを構成し、4種類のイメージをコミットSHAで登録する。
- ReactとBlazorの各イメージを二つの画面へ配置する。画面の接続先は環境変数で固定し、APIは各実装につき1サービスとする。
- 画面はHTTPSで公開し、APIのingressは環境内に限定する。画面の `/api` 中継は外部から利用できる。
- 最後に4画面のHTTP応答とAPI healthを確認する。失敗はworkflowの失敗となる。デプロイ全体の自動ロールバックは行わない。

通常のデプロイは `azure-demo` Environmentを使用する。同時デプロイは1件に制限し、実行中のデプロイを新しいpushで中断しない。

## 設定ファイル

`infra/azure/demo.json` に配置先のリソース名、リージョン、SKU、各アプリのCPU・メモリ・最小レプリカ数を置く。現在のリソース名を指定しているため、既存URLを引き継ぐ。

環境を作り直す場合はこのJSONを変更する。レジストリ名にはAzure全体で一意の値が必要になる。別のGitHub Environment名を選ぶときは、workflowの `environment` と `concurrency.group` も合わせる。

サブスクリプションID・テナントID・CI用IDはGitHub Environmentの変数として設定する。パスワードや接続文字列をJSONへ書かない。テンプレートはIncrementalモードで配置し、対象グループ内の別リソースを削除しない。

## 初回セットアップ

PowerShell 7、Azure CLI、GitHub CLIを使用する。Azure CLIとGitHub CLIでは対象のサブスクリプションとリポジトリを操作できるアカウントでサインインする。

```powershell
az login
gh auth login
az bicep install --version v0.49.2

# Azure上の構成差分と、設定するGitHubの信頼条件を確認
./scripts/Initialize-AzureDemo.ps1 -Preview

# リソースとCI用の接続を設定
./scripts/Initialize-AzureDemo.ps1
```

初回の実行者には、対象グループの作成・リソース管理・ロール割り当てと、GitHub Environment・変数の設定権限が必要となる。未登録のAzureリソースプロバイダーも登録する。

セットアップでは以下を設定する。

| 対象 | 権限・設定 |
| --- | --- |
| コンテナのイメージ取得用ID | 対象レジストリのAcrPull |
| CI用のマネージドID | 対象リソースグループのContributor、対象レジストリのAcrPush |
| CI用IDのフェデレーション | このリポジトリの `azure-demo` EnvironmentのOIDC subject |
| GitHub Environment | mainブランチのみ許可 |
| Environment変数 | `AZURE_CLIENT_ID`、`AZURE_TENANT_ID`、`AZURE_SUBSCRIPTION_ID` |

通常のCI用IDにはロールを付与する権限を与えない。初回セットアップを繰り返しても、同じ対象への同じロールを重複作成しない。既存Environmentに異なるブランチ規則や保護設定がある場合は、上書きする前に停止する。

OIDC subjectはGitHubの現在設定から取得する。所有者・リポジトリIDを含む形式にも対応する。独自にカスタマイズしたsubjectは自動で変更しない。[GitHubのOIDC設定](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-azure)、[subject形式](https://docs.github.com/en/actions/reference/security/oidc)

初回セットアップ後、workflowがmainへ入ると自動デプロイを開始できる。新規環境の6サービスは最初のデプロイで作成される。

## 手動デプロイとプレビュー

```powershell
# 現在のイメージを使って構成差分だけを確認
./scripts/Deploy-AzureDemo.ps1 -Preview

# 現在のソースをビルド・登録してデプロイ
./scripts/Deploy-AzureDemo.ps1 -ImageTag <コミットSHA>

# 登録済みのタグを再配置する。過去の版へ戻す場合も使用できる
./scripts/Deploy-AzureDemo.ps1 -ImageTag <登録済みのコミットSHA> -UseExistingImages

# 公開経路を再確認
./scripts/Test-AzureDemo.ps1
```

`-UseExistingImages` は4種類すべてのタグが存在することを先に確認する。現在のソースをそのタグで再ビルドしない。自動化導入前の、実装ごとに異なるタグの組にはこのコマンドをそのまま適用できない。

Windowsのイメージ作成にはWSLのUbuntu内のDockerを使い、`-Distribution` でディストリビューションを変更できる。LinuxではローカルのDockerを直接使う。レジストリへの認証情報は一時ディレクトリに置き、終了時に削除する。

プレビューは変更するリソースとプロパティ名を表示し、設定値は表示しない。Azureが参照式を解決できず差を表示する場合があるため、プレビューだけで無変更を保証しない。初回の取り込みでは、画面の中継先が環境内FQDNのHTTPS URLへ揃う。

## ツール

Bicep CLI 0.49.2と公式Actionsを使用する。Actionsは確認したリリースのコミットSHAで固定する。

| Action | バージョン |
| --- | --- |
| actions/checkout | 7.0.1 |
| actions/setup-node | 7.1.0 |
| actions/setup-dotnet | 6.1.0 |
| Azure/login | 3.1.0 |

いずれもMITライセンス。アプリの実行時パッケージは追加しない。`global.json` は.NET 10 SDKを指定し、CIのNode.jsは既存確認版24.18.0を使う。

## 現在の制約

記録はAPIのメモリ上にある。イメージ更新やスケール停止で記録が消える。全サービスは最大1レプリカで、ASP.NET Core APIのみ最小1レプリカ、他は最小0を維持する。

mainから取り込んだGoogleログイン実装には、画面用のクライアントIDと両APIの `GOOGLE_CLIENT_ID` が必要である。このデプロイ定義はまだそれらを渡さない。公開前に、クライアントIDのビルド・実行設定、Google側の4画面の承認済みJavaScript生成元、公開環境でのログインと利用者別の操作を確認する。ページ・healthの検証だけではログインの動作を保証しない。DB・カスタムドメイン・ポートフォリオのポータルサイトはこの構成の対象外である。

## 検証状況

2026-11-17、WindowsからWSLのDockerを使用し、4イメージのビルド・検証用タグでの登録、既存環境に対するBicepプレビュー、Azure・GitHubの初回設定、4画面の公開経路を確認した。CI用スクリプトではnpm 159件と.NET 161件のテスト、lint・buildが成功した。初期化の再実行でも同じIDを維持し、CI用のロールが対象グループとレジストリに各1件であること、main限定のEnvironment規則と3変数を確認した。公開経路の確認は既存の配置を対象としており、検証用イメージはアプリへ配置していない。

2026-11-18、mainの0fd4fffを統合した版で、WindowsのCI用スクリプトによるnpm 187件・.NET 186件のテスト、lint・buildが成功。WSLの一時コンテナでReact・Nodeのビルド、画面・ReactのJavaScript配信・API healthの中継を確認した。npm監査は実行時依存0件、開発用を含む全体で23件（moderate 11・high 12）であり、依存更新は行っていない。

GitHub上のPR検証結果はPRのChecksを参照する。開発者の指示でPRのマージ・公開更新は保留する。初回の自動デプロイ、LinuxからのAzureデプロイ、空のリソースグループからの新規環境作成は未確認である。
