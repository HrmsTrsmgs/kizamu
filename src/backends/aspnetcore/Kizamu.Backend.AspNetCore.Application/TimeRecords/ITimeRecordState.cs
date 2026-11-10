using Kizamu.Domain;

namespace Kizamu.Backend.AspNetCore.Application.TimeRecords;

/// <summary>
/// API呼び出しをまたいで保持する作業記録の状態を表します。
/// </summary>
public interface ITimeRecordState
{
    /// <summary>
    /// 最後に追加した作業記録を取得します。
    /// </summary>
    TimeRecord? Current { get; }

    /// <summary>
    /// 保持しているすべての作業記録を取得します。
    /// </summary>
    IReadOnlyList<TimeRecord> All { get; }

    /// <summary>
    /// 作業記録の一覧構成を、追加操作と安全に並行してコピーします。
    /// </summary>
    /// <returns>コピー時点の記録を保存順に保持する配列。記録自体はコピーしません。</returns>
    TimeRecord[] CopyAll();

    /// <summary>
    /// 識別子に一致する作業記録を取得します。
    /// </summary>
    /// <param name="id">作業記録の識別子。</param>
    /// <returns>一致する記録。存在しない場合はnull。</returns>
    TimeRecord? Find(Guid id);

    /// <summary>
    /// 作業記録を追加します。
    /// </summary>
    /// <param name="timeRecord">追加する作業記録。</param>
    void Add(TimeRecord timeRecord);
}
