using AwesomeAssertions;

namespace Kizamu.Domain.Tests;

public sealed class DomainDependenciesTests
{
    [Fact]
    public void Domainは実装固有の層やHTTPとJSONを参照しない()
    {
        var references = typeof(TimeRecord).Assembly.GetReferencedAssemblies();

        references.Select(it => it.Name).Should().NotContain(it =>
            it != null && it.StartsWith("Kizamu.", StringComparison.Ordinal));
        references.Select(it => it.Name).Should().NotContain(it =>
            it == "System.Net.Http" || it == "System.Net.Http.Json" || it == "System.Text.Json"
            || (it != null && it.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)));
    }
}
