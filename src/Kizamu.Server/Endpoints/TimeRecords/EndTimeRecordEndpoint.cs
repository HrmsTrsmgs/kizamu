using Kizamu.Application.TimeRecords;

namespace Kizamu.Server.Endpoints.TimeRecords;

/// <summary>
/// 記録終了APIのEndpointを構成します。
/// </summary>
static class EndTimeRecordEndpoint
{
    extension(WebApplication self)
    {
        /// <summary>
        /// 記録終了APIをアプリケーションへ登録します。
        /// </summary>
        internal void MapEndTimeRecordEndpoint()
        {
            self.MapPost("/api/time-records/{id:guid}/end", (Guid id, EndTimeRecord endTimeRecord) =>
                Results.Ok(
                    new EndTimeRecordResponse(
                        endTimeRecord.Execute(id))));
        }
    }
}
