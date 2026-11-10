using AwesomeAssertions;

namespace Kizamu.Domain.Tests;

public sealed class ProjectTests
{
    [Fact]
    public void 指定した名前のプロジェクトを作成できる()
    {
        var tested = new Project("プロジェクトA");

        tested.Name.Should().Be("プロジェクトA");
    }
}
