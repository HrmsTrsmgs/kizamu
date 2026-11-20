using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;

namespace Kizamu.Frontend.Blazor.Presentation.Tests;

public sealed class PresentationDependenciesTests
{
    readonly IEnumerable<string> 参照先 =
        from reference in typeof(TimeRecorderViewModel).Assembly.GetReferencedAssemblies()
        let name = reference.Name
        where name is not null
        select name!;

    [Fact]
    public void BlazorのPresentationが参照するKizamuのアセンブリはDomainだけです()
    {
        var Kizamuの参照先 =
            from name in 参照先
            where name.StartsWith("Kizamu.", StringComparison.Ordinal)
            select name;

        Kizamuの参照先.Should().Equal("Kizamu.Domain");
    }

    [Fact]
    public void BlazorのPresentationはHTTP通信やWebサーバー用のアセンブリを参照しません()
    {
        参照先.Should().NotContain(it => it == "System.Net.Http" || it == "System.Net.Http.Json"
            || it.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
    }

    [Fact]
    public void BlazorのPresentationはJSON処理用のアセンブリを参照しません()
    {
        参照先.Should().NotContain(it => it == "System.Text.Json" || it == "System.Net.Http.Json");
    }
}
