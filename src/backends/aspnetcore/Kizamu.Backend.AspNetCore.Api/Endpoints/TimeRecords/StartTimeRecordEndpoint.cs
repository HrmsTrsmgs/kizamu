using Kizamu.Backend.AspNetCore.Application.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Api.Endpoints.TimeRecords;

/// <summary>
/// 記録開始APIのEndpointを構成します。
/// </summary>
static class StartTimeRecordEndpoint
{
    extension(WebApplication self)
    {
        /// <summary>
        /// 同一オリジンからの記録開始要求を受け付けるAPIをアプリケーションへ登録します。
        /// </summary>
        internal void MapStartTimeRecordEndpoint()
        {
            self.MapPost("/api/time-records/start", IResult (HttpRequest request, TimeRecordStartOperation timeRecordStartOperation) =>
            {
                if (request.Headers["Sec-Fetch-Site"] != "same-origin")
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                return Results.Json(
                    new TimeRecordResponse(
                        timeRecordStartOperation.Execute()),
                    statusCode: StatusCodes.Status201Created);
            });
        }
    }
}
