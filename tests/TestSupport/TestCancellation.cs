namespace Kizamu.Tests;

/// <summary>テスト実行コンテキストへの依存を集約します。</summary>
static class TestCancellation
{
    /// <summary>呼び出し時のテストコンテキストからキャンセルトークンを取得します。</summary>
    public static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
}
