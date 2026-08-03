namespace Kizamu.Server.Endpoints.Health;

/// <summary>
/// ヘルスチェックAPIのEndpointを構成します。
/// </summary>
static class HealthEndpoint
{
    extension(WebApplication self)
    {
        /// <summary>
        /// ヘルスチェックAPIをアプリケーションへ登録します。
        /// </summary>
        internal void MapHealthEndpoint()
        {
            self.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
        }
    }
}
