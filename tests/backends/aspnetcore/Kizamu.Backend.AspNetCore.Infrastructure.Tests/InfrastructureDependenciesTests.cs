using AwesomeAssertions;
using Kizamu.Tests;
using Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Tests;

public sealed class InfrastructureDependenciesTests
{
    [Fact]
    public void Infrastructureが参照するKizamuの層はApplicationとDomainだけです()
    {
        new AssemblyReferences(typeof(InMemoryTimeRecordState)).Kizamu.Should().BeEquivalentTo("Kizamu.Backend.AspNetCore.Application", "Kizamu.Domain");
    }
}
