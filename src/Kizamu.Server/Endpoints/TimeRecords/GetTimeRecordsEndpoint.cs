using Kizamu.Application.TimeRecords;

namespace Kizamu.Server.Endpoints.TimeRecords;

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
                (ITimeRecordState timeRecordState) =>
                    Results.Ok(
                        timeRecordState.All.Select(timeRecord =>
                            new StartTimeRecordResponse(timeRecord.Id, timeRecord.StartedAt))));
        }
    }
}
