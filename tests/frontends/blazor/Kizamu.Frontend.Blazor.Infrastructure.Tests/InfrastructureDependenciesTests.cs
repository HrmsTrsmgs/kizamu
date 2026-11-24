using AwesomeAssertions;
using Kizamu.Tests;
using Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;

namespace Kizamu.Frontend.Blazor.Infrastructure.Tests;

public sealed class InfrastructureDependenciesTests
{
    [Fact]
    public void Blazorの通信実装が参照するKizamuのアセンブリはPresentationとDomainだけです()
    {
        new AssemblyReferences(typeof(TimeRecordApi)).Kizamu.Should().BeEquivalentTo("Kizamu.Frontend.Blazor.Presentation", "Kizamu.Domain");
    }
}
