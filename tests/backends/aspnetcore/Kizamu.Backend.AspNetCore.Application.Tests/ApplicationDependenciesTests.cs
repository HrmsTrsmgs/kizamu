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
    public void ユースケースが参照するKizamuの層はDomainのみ()
    {
        参照先.Where(it => it.StartsWith("Kizamu.", StringComparison.Ordinal)).Should().Equal("Kizamu.Domain");
    }

    [Fact]
    public void ユースケースはHTTPを参照しない()
    {
        参照先.Should().NotContain(it => it == "System.Net.Http" || it == "System.Net.Http.Json"
            || it.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
    }

    [Fact]
    public void ユースケースはJSONを参照しない()
    {
        参照先.Should().NotContain(it => it == "System.Text.Json" || it == "System.Net.Http.Json");
    }
}
