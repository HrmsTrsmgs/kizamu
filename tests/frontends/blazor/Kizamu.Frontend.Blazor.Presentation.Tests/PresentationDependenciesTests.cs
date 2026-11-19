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
    public void Presentationのアプリケーション依存先はDomainだけである()
    {
        参照先.Where(it => it.StartsWith("Kizamu.", StringComparison.Ordinal))
            .Should().Equal("Kizamu.Domain");
    }

    [Fact]
    public void PresentationはHTTPを参照しない()
    {
        参照先.Should().NotContain(it => it == "System.Net.Http" || it == "System.Net.Http.Json"
            || it.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
    }

    [Fact]
    public void PresentationはJSONを参照しない()
    {
        参照先.Should().NotContain(it => it == "System.Text.Json" || it == "System.Net.Http.Json");
    }
}
