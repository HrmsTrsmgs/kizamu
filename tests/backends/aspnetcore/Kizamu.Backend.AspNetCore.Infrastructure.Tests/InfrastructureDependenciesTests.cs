using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Tests;

public sealed class InfrastructureDependenciesTests
{
    [Fact]
    public void Infrastructureが参照するKizamuの層はApplicationとDomainだけです()
    {
        typeof(InMemoryTimeRecordState).Assembly.GetReferencedAssemblies()
            .Select(it => it.Name).Where(it => it is not null && it.StartsWith("Kizamu.", StringComparison.Ordinal))
            .Should().BeEquivalentTo("Kizamu.Backend.AspNetCore.Application", "Kizamu.Domain");
    }
}
