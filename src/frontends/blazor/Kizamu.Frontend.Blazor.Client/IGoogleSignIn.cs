namespace Kizamu.Frontend.Blazor.Client;

/// <summary>Googleログインボタンを表示し、認証結果を通知します。</summary>
public interface IGoogleSignIn
{
    /// <summary>指定した要素にボタンを表示し、取得したIDトークンを通知します。</summary>
    /// <param name="container">ボタンを表示する要素。</param>
    /// <param name="onCredential">取得したIDトークンを受け取る操作。</param>
    Task RenderButtonAsync(Microsoft.AspNetCore.Components.ElementReference container, Action<string> onCredential);
}
