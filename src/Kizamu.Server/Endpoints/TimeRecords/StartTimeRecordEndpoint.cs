using Kizamu.Application.TimeRecords;

namespace Kizamu.Server.Endpoints.TimeRecords;

/// <summary>
/// 記録開始APIのEndpointを構成します。
/// </summary>
static class StartTimeRecordEndpoint
{
    extension(WebApplication self)
    {
        /// <summary>
        /// 記録開始APIをアプリケーションへ登録します。
        /// </summary>
        internal void MapStartTimeRecordEndpoint()
        {
            self.MapPost("/api/time-records/start", (StartTimeRecord startTimeRecord) =>
                Results.Json(
                    new StartTimeRecordResponse(
                        startTimeRecord.Execute()),
                    statusCode: StatusCodes.Status201Created));
        }
    }
}
