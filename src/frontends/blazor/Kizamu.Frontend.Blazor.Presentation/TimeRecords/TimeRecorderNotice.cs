namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>記録の操作欄で案内する内容を表します。</summary>
public enum TimeRecorderNotice
{
    /// <summary>通知を表示しません。</summary>
    None,

    /// <summary>記録操作の失敗を知らせます。</summary>
    OperationFailed,

    /// <summary>最新の記録を再取得するよう案内します。</summary>
    StateUnconfirmed,
}
