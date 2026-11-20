using AwesomeAssertions;

namespace Kizamu.Backend.AspNetCore.Api.Tests;

public sealed class ApiDependenciesTests
{
    [Fact]
    public void 業務APIが参照するKizamuの層はApplicationとInfrastructureだけです()
    {
        var Kizamuの参照先 =
            from reference in typeof(Program).Assembly.GetReferencedAssemblies()
            select reference.Name into name
            where name is not null && name.StartsWith("Kizamu.", StringComparison.Ordinal)
            select name;

        Kizamuの参照先.Should().BeEquivalentTo("Kizamu.Backend.AspNetCore.Application", "Kizamu.Backend.AspNetCore.Infrastructure");
    }
}
