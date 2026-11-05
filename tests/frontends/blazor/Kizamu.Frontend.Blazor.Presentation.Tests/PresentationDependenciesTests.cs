using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;

namespace Kizamu.Frontend.Blazor.Presentation.Tests;

public sealed class PresentationDependenciesTests
{
    [Fact]
    public void 画面と通信の実装を参照せずアプリケーションの依存先はDomainだけである()
    {
        var references = typeof(TimeRecorderViewModel).Assembly.GetReferencedAssemblies();

        references.Select(it => it.Name).Where(it => it is not null && it.StartsWith("Kizamu.", StringComparison.Ordinal))
            .Should().Equal("Kizamu.Domain");
        references.Select(it => it.Name).Should().NotContain(it =>
            it == "System.Net.Http" || it == "System.Net.Http.Json" || it == "System.Text.Json"
            || (it != null && it.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)));
    }
}
