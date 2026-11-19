using AwesomeAssertions;

namespace Kizamu.Backend.AspNetCore.Api.Tests;

public sealed class ApiDependenciesTests
{
    [Fact]
    public void 業務APIが参照するKizamuの層はApplicationとInfrastructureだけです()
    {
        typeof(Program).Assembly.GetReferencedAssemblies()
            .Select(it => it.Name).Where(it => it is not null && it.StartsWith("Kizamu.", StringComparison.Ordinal))
            .Should().BeEquivalentTo("Kizamu.Backend.AspNetCore.Application", "Kizamu.Backend.AspNetCore.Infrastructure");
    }
}
