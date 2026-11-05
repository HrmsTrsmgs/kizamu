using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Application.Tests;

public sealed class ApplicationDependenciesTests
{
    [Fact]
    public void ユースケースはAPIと保存実装を参照せずDomainだけに依存する()
    {
        var references = typeof(StartTimeRecord).Assembly.GetReferencedAssemblies();

        references.Select(it => it.Name).Where(it => it is not null && it.StartsWith("Kizamu.", StringComparison.Ordinal))
            .Should().Equal("Kizamu.Domain");
        references.Select(it => it.Name).Should().NotContain(it =>
            it == "System.Net.Http" || it == "System.Net.Http.Json" || it == "System.Text.Json"
            || (it != null && it.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)));
    }
}
