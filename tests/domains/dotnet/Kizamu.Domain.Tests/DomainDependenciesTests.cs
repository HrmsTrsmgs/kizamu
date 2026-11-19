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
    public void Domainは実装固有の層を参照しない()
    {
        参照先.Should().NotContain(it => it.StartsWith("Kizamu.", StringComparison.Ordinal));
    }

    [Fact]
    public void DomainはHTTPを参照しない()
    {
        参照先.Should().NotContain(it => it == "System.Net.Http" || it == "System.Net.Http.Json"
            || it.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
    }

    [Fact]
    public void DomainはJSONを参照しない()
    {
        参照先.Should().NotContain(it => it == "System.Text.Json" || it == "System.Net.Http.Json");
    }
}
