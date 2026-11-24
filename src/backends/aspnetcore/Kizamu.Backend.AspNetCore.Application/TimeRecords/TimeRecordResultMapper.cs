using Kizamu.Domain;
using Riok.Mapperly.Abstractions;

namespace Kizamu.Backend.AspNetCore.Application.TimeRecords;

/// <summary>Domainの作業記録から、各ユースケースが返す結果を組み立てます。</summary>
[Mapper]
static partial class TimeRecordResultMapper
{
    /// <summary>開始結果には識別子と開始時刻を含めます。</summary>
    [MapperIgnoreSource(nameof(TimeRecord.EndedAt))]
    internal static partial StartTimeRecordResult ToStartResult(TimeRecord record);

    /// <summary>終了操作で確定した終了時刻を結果に含めます。</summary>
    [MapperIgnoreSource(nameof(TimeRecord.EndedAt))]
    internal static partial EndTimeRecordResult ToEndResult(TimeRecord record, DateTimeOffset endedAt);

    /// <summary>一覧用の結果に記録の時刻を写します。記録中の終了時刻はnullです。</summary>
    internal static partial GetTimeRecordResult ToGetResult(TimeRecord record);
}
