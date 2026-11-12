using Kizamu.Backend.AspNetCore.Infrastructure.Authentication;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Api.Endpoints.TimeRecords;

/// <summary>
/// 作業記録一覧APIのEndpointを構成します。
/// </summary>
static class GetTimeRecordsEndpoint
{
    extension(WebApplication self)
    {
        /// <summary>
        /// 作業記録一覧APIをアプリケーションへ登録します。
        /// </summary>
        internal void MapGetTimeRecordsEndpoint()
        {
            self.MapGet(
                "/api/time-records",
                async (HttpRequest request, TimeRecordReader timeRecordReader, GoogleIdTokenVerifier verifier) =>
                {
                    var authorization = request.Headers.Authorization.ToString();
                    if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        || await verifier.VerifyAsync(authorization[7..]) is null)
                        return Results.Unauthorized();

                    return Results.Ok(timeRecordReader.Execute().Select(it => new TimeRecordResponse(it)));
                });
        }
    }
}
