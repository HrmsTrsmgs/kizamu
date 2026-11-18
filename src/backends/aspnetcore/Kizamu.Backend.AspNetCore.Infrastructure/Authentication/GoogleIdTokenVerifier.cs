using Google.Apis.Auth;
using Google.Apis.Http;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Authentication;

/// <summary>
/// Google IDトークンを検証し、利用者の安定した識別子を取得します。
/// </summary>
public sealed class GoogleIdTokenVerifier
{
    readonly SignedTokenVerificationOptions options;

    /// <summary>
    /// 対象アプリとGoogle公開鍵を取得するHTTP通信を指定します。
    /// </summary>
    /// <param name="clientId">このアプリのGoogleクライアントID。</param>
    /// <param name="publicKeyClient">公開鍵取得用のHTTPクライアント。所有権は呼び出し側にあります。</param>
    public GoogleIdTokenVerifier(string clientId, HttpClient publicKeyClient)
    {
        options = new()
        {
            TrustedAudiences = { clientId },
            TrustedIssuers = { "https://accounts.google.com", "accounts.google.com" },
            HttpClientFactory = new PublicKeyClientFactory(publicKeyClient)
        };
    }

    /// <summary>
    /// 署名、Googleの発行元、対象アプリ、有効期限を検証します。
    /// </summary>
    /// <param name="idToken">Googleから取得したIDトークン。</param>
    /// <returns>検証済みのsub。不正なトークンやsubがない場合はnull。</returns>
    public async Task<string?> VerifyAsync(string idToken)
    {
        try
        {
            return (await JsonWebSignature.VerifySignedTokenAsync(idToken, options)).Subject;
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }

    // 公式ライブラリのHTTP境界を、呼び出し側が所有する公開鍵取得クライアントへ接続します。
    sealed class PublicKeyClientFactory(HttpClient client) : Google.Apis.Http.IHttpClientFactory
    {
        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args) => new(new(new PublicKeyHandler(client)));
    }

    sealed class PublicKeyHandler(HttpClient client) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            client.GetAsync(request.RequestUri, cancellationToken);
    }
}
