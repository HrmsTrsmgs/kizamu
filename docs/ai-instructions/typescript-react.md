# TypeScript/React開発指示

通常の技術規約は共通Skill `typescript-react-conventions` を使う。この文書の共通規約は未導入環境向けの参照版。UIの日本語表示、Vitest・React Testing Library、npm workspaces、Vite・Visual Studioの起動統合はKizamu固有の補足として維持する。

Nodeの業務APIはFastifyを使い、HTTP契約はVitestとFastifyの `inject` で検証する。対象パッケージは `kizamu-backend-node` と `kizamu-backend-node-tests`。ルートのlint・test・buildにはReactとNodeの両方を含める。Nodeへ画面配信やフロント側のAPI中継を持ち込まない。

## TypeScriptとReact

* TypeScriptのstrict設定、既存ESLint設定、Reactの標準的な英語命名規則に従う。
* Reactコンポーネントへ業務ロジックや通信処理の詳細を集中させない。
* 一方で、必要性が確認できない汎用APIクライアント、状態管理、共通コンポーネント、カスタムフックを先行実装しない。
* API URLを画面コンポーネントへ直接埋め込まず、開発時はVite proxyまたは確認済みの標準統合機能を使う。
* `any`、型アサーション、非nullアサーションで不確かな状態を隠さない。
* `useEffect` の非同期処理では、アンマウント後の更新など副作用の寿命を確認する。
* UIの標準表示言語と利用者向け文言は日本語にする。
* 要求されていないReact Router、状態管理、UIライブラリ、CSSフレームワークを追加しない。

## テスト

* VitestとReact Testing Libraryを使い、利用者が観測する表示と操作を優先して検証する。
* 通信境界を差し替え、テストから実際の外部通信を行わない。
* 実時間待機、テスト実行順序、開発者固有の環境へ依存しない。
* 実装内部のstateやprivate相当の処理を直接検査するために設計を歪めない。
* 成功だけでなく、要求された失敗表示や境界状態を確認する。
* Viteデモ要素など、削除が要件となる要素が残っていないことを確認する。

## npm、Vite、検証

* 作業前に `package.json`、lockfile、実在するnpm script、Node.jsとnpmのバージョンを確認する。
* `package-lock.json` をコミット対象とし、通常は `npm ci` で再現可能なインストールを確認する。
* npmパッケージを必要性なく追加、更新、移動、削除しない。
* 変更後は対象テストに加えて、`npm run lint`、`npm test`、`npm run build` を実行する。
* Vite proxyの変更では、設定値だけでなく可能なら実起動してAPI疎通を確認する。
* Visual Studioとの起動統合を変更した場合は、CLI成功だけでVisual Studio操作も確認済みと扱わず、ユーザーによる実機確認と区別する。
