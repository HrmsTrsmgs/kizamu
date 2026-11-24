using AwesomeAssertions;
using Kizamu.Tests;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;

namespace Kizamu.Frontend.Blazor.Presentation.Tests;

public sealed class PresentationDependenciesTests
{
    readonly AssemblyReferences 参照先 = new(typeof(TimeRecorderViewModel));

    [Fact]
    public void BlazorのPresentationが参照するKizamuのアセンブリはDomainだけです()
    {
        参照先.Kizamu.Should().Equal("Kizamu.Domain");
    }

    [Fact]
    public void BlazorのPresentationはHTTP通信やWebサーバー用のアセンブリを参照しません()
    {
        参照先.Http.Should().BeEmpty();
    }

    [Fact]
    public void BlazorのPresentationはJSON処理用のアセンブリを参照しません()
    {
        参照先.Json.Should().BeEmpty();
    }
}
