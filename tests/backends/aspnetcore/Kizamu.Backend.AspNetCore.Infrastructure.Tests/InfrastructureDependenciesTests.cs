using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Tests;

public sealed class InfrastructureDependenciesTests
{
    [Fact]
    public void 保存実装はAPIを参照せずApplicationとDomainに依存する()
    {
        typeof(InMemoryTimeRecordState).Assembly.GetReferencedAssemblies()
            .Select(it => it.Name).Where(it => it is not null && it.StartsWith("Kizamu.", StringComparison.Ordinal))
            .Should().BeEquivalentTo("Kizamu.Backend.AspNetCore.Application", "Kizamu.Domain");
    }
}
