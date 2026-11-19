using AwesomeAssertions;
using Kizamu.Domain;

namespace Kizamu.Domain.Tests;

public sealed class TimeRecordTests
{
    static readonly Guid 記録ID = Guid.Parse("019cde5b-4c00-7000-8000-000000000001");
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 終了時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);

    readonly TimeRecord tested = TimeRecord.Start(開始時刻);

    public static TheoryData<DateTimeOffset?> 復元する終了時刻 => [(DateTimeOffset?)null, 終了時刻];

    [Theory]
    [MemberData(nameof(復元する終了時刻))]
    public void 記録を復元すると識別子と開始時刻と終了時刻が指定した値のまま保たれます(DateTimeOffset? endedAt)
    {
        var 復元した記録 = new TimeRecord(記録ID, 開始時刻, endedAt);

        復元した記録.Id.Should().Be(記録ID);
        復元した記録.StartedAt.Should().Be(開始時刻);
        復元した記録.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public void 識別子と開始時刻と終了時刻が同じ記録は等しくなります()
    {
        var 記録 = new TimeRecord(記録ID, 開始時刻, 終了時刻);
        var 復元した記録 = new TimeRecord(記録ID, 開始時刻, 終了時刻);

        (記録 == 復元した記録).Should().BeTrue();
    }

    [Fact]
    public void 識別子だけが異なる記録は等しくなりません()
    {
        var 別の記録ID = Guid.Parse("019cde5b-4c00-7000-8000-000000000002");
        var 記録 = new TimeRecord(記録ID, 開始時刻, 終了時刻);
        var 比較する記録 = new TimeRecord(別の記録ID, 開始時刻, 終了時刻);

        (記録 == 比較する記録).Should().BeFalse();
    }

    [Fact]
    public void 開始時刻だけが異なる記録は等しくなりません()
    {
        var 記録 = new TimeRecord(記録ID, 開始時刻, 終了時刻);
        var 比較する記録 = new TimeRecord(記録ID, 開始時刻.AddMinutes(1), 終了時刻);

        (記録 == 比較する記録).Should().BeFalse();
    }

    [Fact]
    public void 終了時刻だけが異なる記録は等しくなりません()
    {
        var 記録 = new TimeRecord(記録ID, 開始時刻, 終了時刻);
        var 比較する記録 = new TimeRecord(記録ID, 開始時刻, 終了時刻.AddMinutes(1));

        (記録 == 比較する記録).Should().BeFalse();
    }

    [Fact]
    public void 記録を開始すると空でない識別子が割り当てられます()
    {
        tested.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void 同じ時刻に開始した二つの記録には異なる識別子が割り当てられます()
    {
        var 二つ目の記録 = TimeRecord.Start(開始時刻);

        二つ目の記録.Id.Should().NotBe(tested.Id);
    }

    [Fact]
    public void 記録を開始すると指定した時刻が開始時刻になります()
    {
        tested.StartedAt.Should().Be(開始時刻);
    }

    [Fact]
    public void 記録の開始直後は終了時刻が未設定です()
    {
        tested.EndedAt.Should().BeNull();
    }

    [Fact]
    public void 記録中の経過時間は開始から指定した時刻までです()
    {
        tested.DurationAt(開始時刻.AddHours(26).AddMinutes(15)).Should().Be(new TimeSpan(26, 15, 0));
    }

    [Fact]
    public void 終了済みの記録の経過時間は指定した時刻にかかわらず開始から終了までです()
    {
        tested.End(終了時刻).DurationAt(終了時刻.AddDays(1)).Should().Be(TimeSpan.FromMinutes(90));
    }

    [Fact]
    public void 記録を終了すると指定した時刻で終了した記録が返り元の記録の終了時刻は未設定のままです()
    {
        var 終了した記録 = tested.End(終了時刻);

        終了した記録.Id.Should().Be(tested.Id);
        終了した記録.StartedAt.Should().Be(開始時刻);
        終了した記録.EndedAt.Should().Be(終了時刻);
        tested.EndedAt.Should().BeNull();
    }
}
