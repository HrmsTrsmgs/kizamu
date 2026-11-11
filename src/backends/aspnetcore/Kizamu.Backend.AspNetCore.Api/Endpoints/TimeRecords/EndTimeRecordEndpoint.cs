using Kizamu.Backend.AspNetCore.Application.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Api.Endpoints.TimeRecords;

/// <summary>
/// 記録終了APIのEndpointを構成します。
/// </summary>
static class EndTimeRecordEndpoint
{
    extension(WebApplication self)
    {
        /// <summary>
        /// 同一オリジンからの記録終了要求を受け付けるAPIをアプリケーションへ登録します。
        /// </summary>
        internal void MapEndTimeRecordEndpoint()
        {
            self.MapPost("/api/time-records/{id:guid}/end", IResult (Guid id, HttpRequest request, TimeRecordEndOperation timeRecordEndOperation) =>
            {
                if (request.Headers["Sec-Fetch-Site"] != "same-origin")
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                return Results.Ok(
                    new TimeRecordResponse(
                        timeRecordEndOperation.Execute(id)));
            });
        }
    }
}
