using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Kizamu.Frontend.Blazor.Client;

/// <summary>Google Identity Servicesのログインボタンを表示します。</summary>
public sealed class GoogleSignIn(IJSRuntime js, IConfiguration configuration) : IGoogleSignIn, IDisposable
{
    DotNetObjectReference<GoogleSignIn>? reference;
    Action<string>? onCredential;

    /// <inheritdoc />
    public async Task RenderButtonAsync(ElementReference container, Action<string> onCredential)
    {
        this.onCredential = onCredential;
        reference = DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("kizamuGoogleSignIn", container, configuration["Google:ClientId"] ?? "", reference);
    }

    /// <summary>Googleから受け取ったIDトークンを通知します。</summary>
    /// <param name="credential">GoogleのIDトークン。</param>
    [JSInvokable]
    public void ReceiveCredential(string credential) => onCredential?.Invoke(credential);

    /// <inheritdoc />
    public void Dispose() => reference?.Dispose();
}
