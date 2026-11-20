using Google.Apis.Auth;
using Google.Apis.Http;
using Newtonsoft.Json;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Authentication;

/// <summary>
/// Google IDトークンを検証し、利用者の安定した識別子を取得します。
/// </summary>
public sealed class GoogleIdTokenVerifier
{
    readonly Func<string, Task<JsonWebToken.Payload>> verifySignedToken;
    readonly TimeProvider timeProvider;

    /// <summary>
    /// 対象アプリとGoogle公開鍵を取得するHTTP通信を指定します。
    /// </summary>
    /// <param name="clientId">このアプリのGoogleクライアントID。</param>
    /// <param name="publicKeyClient">公開鍵取得用のHTTPクライアント。所有権は呼び出し側にあります。</param>
    public GoogleIdTokenVerifier(string clientId, HttpClient publicKeyClient)
    {
        var options = new SignedTokenVerificationOptions()
        {
            TrustedAudiences = { clientId },
            TrustedIssuers = { "https://accounts.google.com", "accounts.google.com" },
            HttpClientFactory = new PublicKeyClientFactory(publicKeyClient)
        };
        verifySignedToken = async idToken => await JsonWebSignature.VerifySignedTokenAsync<GoogleIdentityPayload>(idToken, options);
        timeProvider = TimeProvider.System;
    }

    internal GoogleIdTokenVerifier(Func<string, Task<JsonWebToken.Payload>> verifySignedToken, TimeProvider timeProvider)
    {
        this.verifySignedToken = verifySignedToken;
        this.timeProvider = timeProvider;
    }

    /// <summary>
    /// 署名、Googleの発行元、対象アプリ、有効期限を検証します。
    /// </summary>
    /// <param name="idToken">Googleから取得したIDトークン。</param>
    /// <returns>検証済みの空でない文字列のsub。トークンやsubが無効な場合はnull。</returns>
    public async Task<string?> VerifyAsync(string idToken)
    {
        try
        {
            var payload = await verifySignedToken(idToken);
            if (payload.ExpirationTimeSeconds is not long expiresAt || timeProvider.GetUtcNow().ToUnixTimeSeconds() >= expiresAt)
                return null;

            return payload.Subject;
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

[JsonConverter(typeof(GoogleIdentityPayloadConverter))]
internal sealed class GoogleIdentityPayload : JsonWebSignature.Payload
{
    // JSONの数値を文字列に変換せず、元の型で本人識別子を判定します。
    [JsonProperty("sub", NullValueHandling = NullValueHandling.Include)]
    object? SubjectClaim
    {
        set => Subject = value is string { Length: > 0 } identifier ? identifier : null;
    }
}

sealed class GoogleIdentityPayloadConverter : JsonConverter<GoogleIdentityPayload>
{
    public override bool CanWrite => false;

    public override GoogleIdentityPayload ReadJson(
        JsonReader reader, Type objectType, GoogleIdentityPayload? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var dateParseHandling = reader.DateParseHandling;
        try
        {
            // 日付形式の文字列も、本人識別子として元の表記を保ちます。
            reader.DateParseHandling = DateParseHandling.None;
            var payload = new GoogleIdentityPayload();
            serializer.Populate(reader, payload);
            return payload;
        }
        finally
        {
            reader.DateParseHandling = dateParseHandling;
        }
    }

    public override void WriteJson(JsonWriter writer, GoogleIdentityPayload? value, JsonSerializer serializer) =>
        throw new NotSupportedException();
}
