using AwesomeAssertions;

namespace Kizamu.Domain.Tests;

public sealed class ProjectTests
{
    [Fact]
    public void プロジェクトを作成すると指定した名前が設定されます()
    {
        var tested = new Project("プロジェクトA");

        tested.Name.Should().Be("プロジェクトA");
    }
}
