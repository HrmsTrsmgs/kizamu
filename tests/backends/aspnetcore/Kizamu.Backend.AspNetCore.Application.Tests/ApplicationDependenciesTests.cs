using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Application.Tests;

public sealed class ApplicationDependenciesTests
{
    readonly IEnumerable<string> 参照先 =
        from reference in typeof(TimeRecordStartOperation).Assembly.GetReferencedAssemblies()
        let name = reference.Name
        where name is not null
        select name!;

    [Fact]
    public void Applicationが参照するKizamuの層はDomainだけです()
    {
        var Kizamuの参照先 =
            from name in 参照先
            where name.StartsWith("Kizamu.", StringComparison.Ordinal)
            select name;

        Kizamuの参照先.Should().Equal("Kizamu.Domain");
    }

    [Fact]
    public void ApplicationはHTTP関連のアセンブリを参照しません()
    {
        参照先.Should().NotContain(it => it == "System.Net.Http" || it == "System.Net.Http.Json"
            || it.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplicationはJSON関連のアセンブリを参照しません()
    {
        参照先.Should().NotContain(it => it == "System.Text.Json" || it == "System.Net.Http.Json");
    }
}
