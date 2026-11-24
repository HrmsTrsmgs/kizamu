using AwesomeAssertions;
using Kizamu.Tests;

namespace Kizamu.Domain.Tests;

public sealed class DomainDependenciesTests
{
    readonly AssemblyReferences 参照先 = new(typeof(TimeRecord));

    [Fact]
    public void DomainはKizamuの他のアセンブリを参照しません()
    {
        参照先.Kizamu.Should().BeEmpty();
    }

    [Fact]
    public void DomainはHTTP関連のアセンブリを参照しません()
    {
        参照先.Http.Should().BeEmpty();
    }

    [Fact]
    public void DomainはJSON関連のアセンブリを参照しません()
    {
        参照先.Json.Should().BeEmpty();
    }
}
