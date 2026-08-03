using Kizamu.Domain;

namespace Kizamu.Application.TimeRecords;

/// <summary>
/// API呼び出しをまたいで保持する作業記録の状態を表します。
/// </summary>
public interface ITimeRecordState
{
    /// <summary>
    /// 保持している作業記録を取得または設定します。
    /// </summary>
    TimeRecord? Current { get; set; }
}
