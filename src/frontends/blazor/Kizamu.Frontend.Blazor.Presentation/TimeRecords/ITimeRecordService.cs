using System.Collections.Immutable;
using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>画面の記録操作を提供します。通信形式は境界の内側でDomainの記録へ変換します。</summary>
public interface ITimeRecordService
{
    /// <summary>記録中と終了済みの作業記録を取得します。</summary>
    /// <returns>取得した順序の作業記録一覧。</returns>
    Task<ImmutableArray<TimeRecord>> GetTimeRecordsAsync();

    /// <summary>プロジェクト未指定の記録を開始します。</summary>
    /// <returns>開始時刻が確定した作業記録。</returns>
    Task<TimeRecord> StartTimeRecordAsync();

    /// <summary>指定した作業記録を終了します。</summary>
    /// <param name="id">終了する記録の識別子。</param>
    /// <returns>終了時刻が確定した作業記録。</returns>
    Task<TimeRecord> EndTimeRecordAsync(Guid id);
}
