using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;
using Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Tests.TimeRecords;

public sealed class InMemoryTimeRecordStateTests
{
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 次の開始時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);
    readonly InMemoryTimeRecordState tested = new();
    readonly TimeRecord 最初の記録 = TimeRecord.Start(開始時刻);
    readonly TimeRecord 次の記録 = TimeRecord.Start(次の開始時刻);

    [Fact]
    public void 作業記録を一件保存すると現在の記録として同じインスタンスを取得できます()
    {
        tested.Add(最初の記録);

        tested.Current.Should().BeSameAs(最初の記録);
    }

    [Fact]
    public void 複数の作業記録から識別子で検索すると該当する記録と同じインスタンスを取得できます()
    {
        tested.Add(最初の記録);
        tested.Add(次の記録);

        tested.Find(最初の記録.Id).Should().BeSameAs(最初の記録);
    }

    [Fact]
    public void 作業記録を追加していない場合は空の一覧を取得できます()
    {
        tested.All.Should().BeEmpty();
    }

    [Fact]
    public void 作業記録を一件追加すると一覧にはその記録だけが含まれます()
    {
        tested.Add(最初の記録);

        tested.All.Should().Equal(最初の記録);
    }

    [Fact]
    public void 作業記録を二件追加すると一覧には両方の記録が追加順に並びます()
    {
        tested.Add(最初の記録);
        tested.Add(次の記録);

        tested.All.Should().Equal(最初の記録, 次の記録);
    }

    [Fact]
    public void 作業記録を終了済みに更新すると一覧での位置は変わらず終了時刻が反映されます()
    {
        tested.Add(最初の記録);
        tested.Add(次の記録);
        var 終了した記録 = 最初の記録.End(次の開始時刻);

        tested.Update(終了した記録);

        tested.All.Should().Equal(終了した記録, 次の記録);
    }

    [Fact]
    public void 未保存の記録を更新するとTimeRecordNotFoundExceptionが投げられ保存済みの記録は変わりません()
    {
        tested.Add(最初の記録);

        var 更新する = () => tested.Update(次の記録);
        更新する.Should().ThrowExactly<TimeRecordNotFoundException>();

        tested.All.Should().ContainSingle().Which.Should().BeSameAs(最初の記録);
    }

    [Fact]
    public void 記録一覧をコピーしてから記録を追加してもコピーには元の記録インスタンスだけが含まれます()
    {
        tested.Add(最初の記録);

        var コピー = tested.CopyAll();
        tested.Add(次の記録);

        コピー.Should().ContainSingle().Which.Should().BeSameAs(最初の記録);
    }

    [Fact]
    public void 記録一覧をコピーした後に追加した記録は元の一覧に反映されます()
    {
        tested.Add(最初の記録);
        var 一覧 = tested.All;

        tested.CopyAll();
        tested.Add(次の記録);

        一覧.Should().Equal(最初の記録, 次の記録);
    }

    [Fact]
    public void 同じ一覧を列挙するたびに列挙開始前に追加された記録を取得できます()
    {
        var 一覧 = tested.All;
        tested.Add(最初の記録);

        一覧.Should().Equal(最初の記録);

        tested.Add(次の記録);

        一覧.Should().Equal(最初の記録, 次の記録);
    }

    [Fact]
    public void 記録一覧を取得した後に記録を追加すると取得済みの一覧の件数が増えます()
    {
        var 一覧 = tested.All;

        tested.Add(最初の記録);
        tested.Add(次の記録);

        一覧.Count.Should().Be(2);
    }

    [Fact]
    public void 記録一覧を取得した後に追加した記録も元の一覧から添字を指定して追加順に取得できます()
    {
        var 一覧 = tested.All;

        tested.Add(最初の記録);
        tested.Add(次の記録);

        一覧[0].Should().BeSameAs(最初の記録);
        一覧[1].Should().BeSameAs(次の記録);
    }
}
