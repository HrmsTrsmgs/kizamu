using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Kizamu.Frontend.Blazor.Client.Tests;

public sealed class GoogleSignInInteropTests
{
    const string ログインボタンを表示するJS関数 = "kizamuGoogleSignIn";
    readonly BunitJSInterop js = new();

    public GoogleSignInInteropTests() =>
        js.SetupVoid(ログインボタンを表示するJS関数, _ => true).SetVoidResult();

    [Fact]
    public async Task ログインボタンを二度表示しても同じコールバック参照をJSに渡します()
    {
        using var tested = Googleログインを作る();

        await tested.RenderButtonAsync(default, _ => { });
        await tested.RenderButtonAsync(default, _ => { });

        コールバック参照(1).Should().BeSameAs(コールバック参照(0));
    }

    [Fact]
    public async Task ログインボタンの再表示後は最新の通知先に一回だけ結果を渡します()
    {
        using var tested = Googleログインを作る();
        List<string> 最初の通知 = [];
        List<string> 最新の通知 = [];

        await tested.RenderButtonAsync(default, 最初の通知.Add);
        await tested.RenderButtonAsync(default, 最新の通知.Add);
        コールバック参照(1).Value.ReceiveCredential("google-id-token");

        最初の通知.Should().BeEmpty();
        最新の通知.Should().Equal("google-id-token");
    }

    [Fact]
    public async Task Googleログインの破棄時にJSへ渡したコールバック参照を解放します()
    {
        var tested = Googleログインを作る();
        await tested.RenderButtonAsync(default, _ => { });
        var 参照 = コールバック参照(0);

        tested.Dispose();

        var 参照を開く = () => 参照.Value;
        参照を開く.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task Googleログインの破棄後に届いた結果は以前の通知先へ渡しません()
    {
        var tested = Googleログインを作る();
        List<string> 通知 = [];
        await tested.RenderButtonAsync(default, 通知.Add);
        var 結果を通知する = コールバック参照(0).Value.ReceiveCredential;

        tested.Dispose();
        結果を通知する("google-id-token");

        通知.Should().BeEmpty();
    }

    Client.GoogleSignIn Googleログインを作る() =>
        new(js.JSRuntime, new ConfigurationBuilder().Build());

    DotNetObjectReference<Client.GoogleSignIn> コールバック参照(int 表示のインデックス) =>
        js.Invocations[ログインボタンを表示するJS関数][表示のインデックス].Arguments[2]
            .Should().BeOfType<DotNetObjectReference<Client.GoogleSignIn>>().Which;
}
