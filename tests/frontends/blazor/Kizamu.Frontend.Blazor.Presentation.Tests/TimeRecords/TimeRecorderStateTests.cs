using AwesomeAssertions;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;

namespace Kizamu.Frontend.Blazor.Presentation.Tests.TimeRecords;

public sealed class TimeRecorderStateTests
{
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");
    readonly TimeRecord 先に開始した記録 = new(Guid.Parse("019cde5b-4c00-7000-8000-000000000001"), 開始時刻, null);
    readonly TimeRecord 次に開始した記録 = new(Guid.Parse("019cdeac-b600-7000-8000-000000000002"), 開始時刻, null);
    readonly TimeRecorderState tested = new();

    [Fact]
    public void 一覧にない記録は各記録の最新状態を取得中に始めた順に追加します()
    {
        var 終了した記録 = 先に開始した記録.End(開始時刻.AddHours(1));

        tested.ApplyLoadedRecords([], [先に開始した記録, 次に開始した記録, 終了した記録]);

        tested.Records.Should().Equal(終了した記録, 次に開始した記録);
    }

    [Fact]
    public void 一覧の取得中に開始した記録は応答の先頭に別の未終了記録があっても終了対象になります()
    {
        tested.ApplyStartedRecord(null, 先に開始した記録);

        tested.ApplyLoadedRecords([次に開始した記録], [先に開始した記録]);

        tested.CurrentId.Should().Be(先に開始した記録.Id);
    }

    [Fact]
    public void プロジェクトの選択中に一覧から未指定の記録が消えると未指定に戻っても終了対象はありません()
    {
        tested.ApplyStartedRecord(null, 先に開始した記録);
        tested.SelectedProject = new("プロジェクトA");
        tested.ApplyStartedRecord(tested.SelectedProject, 次に開始した記録);

        tested.ApplyLoadedRecords([次に開始した記録], []);
        tested.SelectedProject = null;

        tested.CurrentId.Should().BeNull();
    }
}
