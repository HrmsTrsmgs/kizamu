using Kizamu.Backend.AspNetCore.Infrastructure.Authentication;

namespace Kizamu.Backend.AspNetCore.Api.Endpoints.TimeRecords;

static class TimeRecordAuthentication
{
    internal static Task<string?> GetOwnerAsync(HttpRequest request, GoogleIdTokenVerifier verifier)
    {
        var authorization = request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? verifier.VerifyAsync(authorization[7..])
            : Task.FromResult<string?>(null);
    }
}
