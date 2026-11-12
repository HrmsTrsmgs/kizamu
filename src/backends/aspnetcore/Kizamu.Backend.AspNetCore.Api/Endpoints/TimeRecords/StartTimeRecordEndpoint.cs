using System.Collections.Concurrent;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Backend.AspNetCore.Infrastructure.Authentication;

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
        internal void MapStartTimeRecordEndpoint(ConcurrentDictionary<Guid, string> owners)
        {
            self.MapPost("/api/time-records/start", async (HttpRequest request, TimeRecordStartOperation timeRecordStartOperation, GoogleIdTokenVerifier verifier) =>
            {
                var authorization = request.Headers.Authorization.ToString();
                var owner = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? await verifier.VerifyAsync(authorization[7..]) : null;
                if (owner is null) return Results.Unauthorized();
                var record = timeRecordStartOperation.Execute();
                owners[record.Id] = owner;
                return Results.Json(new TimeRecordResponse(record), statusCode: StatusCodes.Status201Created);
            });
        }
    }
}
