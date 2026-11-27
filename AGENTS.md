# Kizamu 作業指示

## 読むべき指示

* 文書だけの変更は [共通の文書変更ルール](docs/ai-instructions/common.md#gitと作業ツリー) に従う。
* 主担当は、作業の登録・進捗・完了とPRとの関連を [GitHub Projectsによる作業管理](docs/ai-instructions/common.md#github-projectsによる作業管理) に従って扱う。
* 方針の検討が必要な場合や、未確認の前提が大きな手戻りにつながる場合は [方針の検討と前提の確認](docs/ai-instructions/common.md#方針の検討と前提の確認) に従う。
* 公開APIの設計・検証では [APIの契約](docs/ai-instructions/common.md#設計と依存関係)、Greenで既存コードを再利用する場合は [適用先での確認](docs/ai-instructions/tdd-context/green.md#既存コードを再利用するときの確認) に従う。
* Refactor担当は、完了前に [変更箇所のルール確認](docs/ai-instructions/tdd-context/refactor.md#変更箇所のルール確認) を行う。
* TypeScriptの型・分岐・テストの書き方は技術別指示に従う。 [コードとテスト](docs/ai-instructions/typescript-react.md#コードとテスト) · [Reactのテスト境界](docs/ai-instructions/typescript-react.md#reactのテスト境界)
* テストの追加・整理・レビューでは、Kizamuの責務分担と記述方針を確認する。手元の共通版に規則が欠けている場合は該当する参照版で補う。 [テストの責務](docs/ai-instructions/kizamu.md#domain画面テストの責務) · [テストの記述方針](docs/testing-style.md) · [テストの設計と準備](docs/ai-instructions/common.md#テストの設計と準備)
* 処理・画面部品・状態を共用するときは、既存の責務と契約を照合する。 [共通化の判断](docs/ai-instructions/common.md#共通化の判断) · [画面部品と状態の共用](docs/ai-instructions/kizamu.md#画面部品と状態の共用)
* 進行役は、委任の可否・統括による代行とRGRのモデル・推論レベルを [モデル方針](docs/ai-instructions/tdd-context/models.md) で確認する。導入済み共通Skillに旧割り当てや代行禁止が残る場合も、この方針を優先する。
* 共通方針は個人共通AGENTSを使う。未導入時は [参照版](docs/ai-instructions/common.md) を読み、手元の版に規則が欠けている場合は同参照版の該当節で補う。
* 振る舞い追加・仕様変更・不具合修正は共通Skill `tdd-workflow`、挙動を維持するリファクタリングの進行は同Skillの `references/run.md` を入口とする。未導入時は [TDDの入口](docs/ai-instructions/tdd.md) を使う。運用文書の編集や性能調査・計測だけでは製品TDDを起動しない。
* C#/.NETは共通Skill `csharp-conventions` と [プロジェクトの補足](docs/ai-instructions/csharp.md#プロジェクトの補足) を使う。未導入時だけ同文書の参照版を読む。
* TypeScript/Reactは共通Skill `typescript-react-conventions` と [プロジェクトの補足](docs/ai-instructions/typescript-react.md#プロジェクトの補足) を使う。未導入時だけ同文書の参照版を読む。
* 固有の仕様・制約は [Kizamuの指示](docs/ai-instructions/kizamu.md) を確認する。工程担当は担当範囲の節だけを読む。

共通Skillと参照版は同じ用途に重ねて読まない。進行役は選んだ進行手順、起動済みの工程担当は同じ側の工程共通と担当工程だけを工程指示として読む。担当の再開時に入口を再実行せず、候補一覧や他工程の議論を渡さない。技術・固有規約は必要な範囲で補足する。私用の共通リポジトリへのアクセスや導入をOSSへの参加条件にしない。

## 仕様・判断と記録先

主担当は開始・再開時に [記録の索引](ai-notes/README.md)、[現在の作業](ai-notes/mvp-implementation-plan.md)、必要な仕様・判断を読み、現在のGit状態と照合する。記録の更新・重複整理は共通AGENTSの「仕様・判断と再開記録」に従う。

| 内容 | 記録先 |
| --- | --- |
| 確定仕様・利用例 | 既存の機能文書・テストの該当箇所 |
| 判断理由・不採用案 | [docs/decisions.md](docs/decisions.md) |
| 未確定仕様・候補 | [ai-notes/development-candidates.md](ai-notes/development-candidates.md)、具体化済みの保留仕様はSkipテスト |
| 現在の工程・停止位置・検証結果・次の着手点 | [ai-notes/mvp-implementation-plan.md](ai-notes/mvp-implementation-plan.md) を置き換え更新 |

再開時は実装計画の「現在の次作業」と判断記録・開発候補を照合し、次の着手点も同欄へ残す。工程担当へは必要な確定契約だけを渡す。

## 指示の更新

他の開発にも共有できる規則は `codex-workflows` の共通AGENTS・技術別Skill・工程別指示を正本とし、未導入環境向け参照版も同期する。製品の仕様、採用パッケージ、検証対象、記録先はこのプロジェクトに残す。一時的な操作ログや未合意の案を共通規則へ昇格させず、他プロジェクト固有のデータ・公開手順・実装を機械的に転用しない。
