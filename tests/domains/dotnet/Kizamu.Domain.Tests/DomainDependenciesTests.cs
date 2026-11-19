using AwesomeAssertions;

namespace Kizamu.Domain.Tests;

public sealed class DomainDependenciesTests
{
    readonly IEnumerable<string> 参照先 =
        from reference in typeof(TimeRecord).Assembly.GetReferencedAssemblies()
        let name = reference.Name
        where name is not null
        select name!;

    [Fact]
    public void DomainはKizamuの他のアセンブリを参照しません()
    {
        参照先.Should().NotContain(it => it.StartsWith("Kizamu.", StringComparison.Ordinal));
    }

    [Fact]
    public void DomainはHTTP関連のアセンブリを参照しません()
    {
        参照先.Should().NotContain(it => it == "System.Net.Http" || it == "System.Net.Http.Json"
            || it.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
    }

    [Fact]
    public void DomainはJSON関連のアセンブリを参照しません()
    {
        参照先.Should().NotContain(it => it == "System.Text.Json" || it == "System.Net.Http.Json");
    }
}
