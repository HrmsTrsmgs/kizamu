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
                (GetTimeRecords getTimeRecords) =>
                    Results.Ok(
                        getTimeRecords.Execute().Select(it =>
                            new TimeRecordResponse(it))));
        }
    }
}
