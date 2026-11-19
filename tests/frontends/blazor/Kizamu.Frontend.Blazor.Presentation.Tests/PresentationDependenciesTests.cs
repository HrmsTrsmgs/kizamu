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
        参照先.Where(it => it.StartsWith("Kizamu.", StringComparison.Ordinal))
            .Should().Equal("Kizamu.Domain");
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
