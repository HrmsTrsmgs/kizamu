using AwesomeAssertions;
using Kizamu.Tests;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Application.Tests;

public sealed class ApplicationDependenciesTests
{
    readonly AssemblyReferences 参照先 = new(typeof(TimeRecordStartOperation));

    [Fact]
    public void Applicationが参照するKizamuの層はDomainだけです()
    {
        参照先.Kizamu.Should().Equal("Kizamu.Domain");
    }

    [Fact]
    public void ApplicationはHTTP関連のアセンブリを参照しません()
    {
        参照先.Http.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationはJSON関連のアセンブリを参照しません()
    {
        参照先.Json.Should().BeEmpty();
    }
}
