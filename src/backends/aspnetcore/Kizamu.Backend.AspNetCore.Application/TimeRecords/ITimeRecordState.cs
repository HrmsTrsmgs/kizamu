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
    /// 保存順の一覧を取得します。取得後の追加も、この一覧の件数とインデクサーに反映されます。
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
    /// 同じ識別子の作業記録を、保存順を保って差し替えます。
    /// </summary>
    /// <param name="timeRecord">更新後の作業記録。</param>
    /// <exception cref="TimeRecordNotFoundException">指定した作業記録が保存先に存在しない場合。</exception>
    void Update(TimeRecord timeRecord);

    /// <summary>
    /// 作業記録を追加します。
    /// </summary>
    /// <param name="timeRecord">追加する作業記録。</param>
    void Add(TimeRecord timeRecord);
}
