using Kizamu.Backend.AspNetCore.Application.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Api.Endpoints.TimeRecords;

/// <summary>
/// 記録開始APIのEndpointを構成します。
/// </summary>
static class StartTimeRecordEndpoint
{
    extension(RouteGroupBuilder self)
    {
        /// <summary>
        /// 記録開始APIをグループへ登録します。
        /// </summary>
        internal void MapStartTimeRecordEndpoint()
        {
            self.MapPost("/api/time-records/start", (TimeRecordStartOperation timeRecordStartOperation) =>
                Results.Json(
                    new TimeRecordResponse(
                        timeRecordStartOperation.Execute()),
                    statusCode: StatusCodes.Status201Created));
        }
    }
}
