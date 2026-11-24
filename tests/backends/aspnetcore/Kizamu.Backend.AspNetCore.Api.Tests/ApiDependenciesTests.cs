using AwesomeAssertions;
using Kizamu.Tests;

namespace Kizamu.Backend.AspNetCore.Api.Tests;

public sealed class ApiDependenciesTests
{
    [Fact]
    public void 業務APIが参照するKizamuの層はApplicationとInfrastructureだけです()
    {
        new AssemblyReferences(typeof(Program)).Kizamu.Should().BeEquivalentTo("Kizamu.Backend.AspNetCore.Application", "Kizamu.Backend.AspNetCore.Infrastructure");
    }
}
