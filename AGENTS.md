# Kizamu 作業指示

このファイルは、CodexなどのAI開発エージェントがこのリポジトリで作業するときの入口である。
詳細な指示は、用途別に `docs/ai-instructions/` 配下へ分割する。

## 読むべき指示

作業開始時は、このファイルに加えて、作業内容に応じた指示を読む。

* 常に適用する共通指示: `docs/ai-instructions/common.md`
* 振る舞い追加、仕様変更、不具合修正、リファクタリング: `docs/ai-instructions/tdd.md`
* 複数チャットでTDDを進行、レビューする場合: `docs/ai-instructions/tdd-orchestration.md`
* C#、.NET、xUnit、AwesomeAssertions、FluentAssertions、NuGet、MSBuild: `docs/ai-instructions/csharp.md`
* TypeScript、React、Vite、Vitest、ESLint、npm: `docs/ai-instructions/typescript-react.md`
* Kizamu固有の製品設計、README、開発環境: `docs/ai-instructions/kizamu.md`

詳細な製品コンセプトは `docs/concept.md` を参照する。

## 分類方針

* 他の開発にも共有できる内容は `common.md` または `tdd.md` に置く。
* C#/.NETまたはTypeScript/Reactに閉じる内容は、それぞれの技術別指示に置く。
* Kizamuの目的、記録操作、管理、集計、作業ツリー、外部連携などの固有方針は `kizamu.md` に置く。
* 今回限りの作業メモ、試行錯誤、すぐ陳腐化する操作ログは指示化しない。
* SpreadsheetAsDataとDocumentAsDataのAI指示から共通・技術別のルールを取り込む際も、Kizamu固有の決定を上書きしない。ライブラリ公開、Ruby版同期、Excel・DOCXテストデータなどの固有運用はそのまま転用しない。

## 優先順位

* ユーザーの明示指示を最優先する。
* ユーザーの最新指示が過去の指示と矛盾する場合は、最新指示を優先する。
* 共通指示とプロジェクト固有指示が矛盾する場合は、プロジェクト固有指示を優先する。
* 技術別指示とTDD指示が矛盾する場合は、ユーザーの現在の目的に近いほうを優先し、判断理由を報告する。

## 最重要ルール

* ユーザーの意図を勝手に弱めたり強めたりしない。
* 未確認の既存API、型、メソッド、設定項目、コマンドを、存在する事実として扱わない。
* 新しいAPI、型、メソッド、設定項目、コマンドは、提案または実装変更として明示し、確認可能な差分にする。
* 依頼範囲外の変更を混ぜない。
* ユーザーの未コミット変更を無断で破棄、上書き、コミットしない。
* 振る舞いを追加または変更する場合は、原則としてRed、Green、Refactorで作業だけでなく考える対象も分ける。各段階の役割と仕様変更の手順は `docs/ai-instructions/tdd.md` に従う。
* Greenでは、対象テストが要求していない仕様を先取りして実装しない。
* コミット前に差分、対象ファイル、コミットメッセージ案を提示し、ユーザーの了承を得る。
* pushは、ユーザーから指示された場合に行う。
