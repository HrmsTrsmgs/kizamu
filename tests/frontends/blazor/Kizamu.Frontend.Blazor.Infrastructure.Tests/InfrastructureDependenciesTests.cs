using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;

namespace Kizamu.Frontend.Blazor.Infrastructure.Tests;

public sealed class InfrastructureDependenciesTests
{
    [Fact]
    public void Blazorの通信実装が参照するKizamuのアセンブリはPresentationとDomainだけです()
    {
        typeof(TimeRecordApi).Assembly.GetReferencedAssemblies()
            .Select(it => it.Name).Where(it => it is not null && it.StartsWith("Kizamu.", StringComparison.Ordinal))
            .Should().BeEquivalentTo("Kizamu.Frontend.Blazor.Presentation", "Kizamu.Domain");
    }
}
