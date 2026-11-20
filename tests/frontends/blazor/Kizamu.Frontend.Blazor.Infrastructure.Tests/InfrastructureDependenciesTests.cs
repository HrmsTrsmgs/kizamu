using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;

namespace Kizamu.Frontend.Blazor.Infrastructure.Tests;

public sealed class InfrastructureDependenciesTests
{
    [Fact]
    public void Blazorの通信実装が参照するKizamuのアセンブリはPresentationとDomainだけです()
    {
        var Kizamuの参照先 =
            from reference in typeof(TimeRecordApi).Assembly.GetReferencedAssemblies()
            select reference.Name into name
            where name is not null && name.StartsWith("Kizamu.", StringComparison.Ordinal)
            select name;

        Kizamuの参照先.Should().BeEquivalentTo("Kizamu.Frontend.Blazor.Presentation", "Kizamu.Domain");
    }
}
