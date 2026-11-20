using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Tests;

public sealed class InfrastructureDependenciesTests
{
    [Fact]
    public void Infrastructureが参照するKizamuの層はApplicationとDomainだけです()
    {
        var Kizamuの参照先 =
            from reference in typeof(InMemoryTimeRecordState).Assembly.GetReferencedAssemblies()
            select reference.Name into name
            where name is not null && name.StartsWith("Kizamu.", StringComparison.Ordinal)
            select name;

        Kizamuの参照先.Should().BeEquivalentTo("Kizamu.Backend.AspNetCore.Application", "Kizamu.Domain");
    }
}
