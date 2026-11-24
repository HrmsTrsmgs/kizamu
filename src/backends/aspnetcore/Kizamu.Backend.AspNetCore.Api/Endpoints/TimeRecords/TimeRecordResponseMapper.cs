using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Riok.Mapperly.Abstractions;

namespace Kizamu.Backend.AspNetCore.Api.Endpoints.TimeRecords;

/// <summary>記録操作の結果からHTTP応答を組み立てます。</summary>
[Mapper]
static partial class TimeRecordResponseMapper
{
    /// <summary>保存済みの記録を一覧の一要素に変換します。記録中の終了時刻はnullです。</summary>
    internal static partial TimeRecordResponse ToResponse(GetTimeRecordResult result);

    /// <summary>開始した記録を変換します。開始応答の終了時刻はnullです。</summary>
    [MapValue(nameof(TimeRecordResponse.EndedAt), null)]
    internal static partial TimeRecordResponse ToResponse(StartTimeRecordResult result);

    /// <summary>終了した記録を、確定した終了時刻を含む応答へ変換します。</summary>
    internal static partial TimeRecordResponse ToResponse(EndTimeRecordResult result);
}
