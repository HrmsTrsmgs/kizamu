using System.Collections.Concurrent;
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
        internal void MapGetTimeRecordsEndpoint(ConcurrentDictionary<Guid, string> owners)
        {
            self.MapGet(
                "/api/time-records",
                async (HttpRequest request, TimeRecordReader timeRecordReader, GoogleIdTokenVerifier verifier) =>
                {
                    var owner = await TimeRecordAuthentication.GetOwnerAsync(request, verifier);
                    if (owner is null) return Results.Unauthorized();

                    var records =
                        from record in timeRecordReader.Execute()
                        where owners.GetValueOrDefault(record.Id) == owner
                        select new TimeRecordResponse(record);
                    return Results.Ok(records);
                });
        }
    }
}
