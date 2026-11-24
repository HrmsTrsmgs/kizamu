using System.Collections.Concurrent;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Backend.AspNetCore.Infrastructure.Authentication;

namespace Kizamu.Backend.AspNetCore.Api.Endpoints.TimeRecords;

/// <summary>
/// 記録終了APIのEndpointを構成します。
/// </summary>
static class EndTimeRecordEndpoint
{
    extension(RouteGroupBuilder self)
    {
        /// <summary>
        /// 記録終了APIをグループへ登録します。
        /// </summary>
        internal void MapEndTimeRecordEndpoint(ConcurrentDictionary<Guid, string> owners)
        {
            self.MapPost("/api/time-records/{id:guid}/end", async (Guid id, HttpRequest request, TimeRecordEndOperation timeRecordEndOperation, GoogleIdTokenVerifier verifier) =>
            {
                var owner = await TimeRecordAuthentication.GetOwnerAsync(request, verifier);
                if (owner is null) return Results.Unauthorized();
                if (!owners.TryGetValue(id, out var recordOwner) || recordOwner != owner) return Results.NotFound();
                try
                {
                    return Results.Ok(TimeRecordResponseMapper.ToResponse(timeRecordEndOperation.Execute(id)));
                }
                catch (TimeRecordNotFoundException)
                {
                    return Results.NotFound();
                }
            });
        }
    }
}
