using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>記録画面に表示する状態と操作を提供します。</summary>
public interface ITimeRecorderViewModel
{
    /// <summary>表示する記録一覧を取得します。</summary>
    IReadOnlyList<TimeRecord> Records { get; }

    /// <summary>記録中かどうかを取得します。</summary>
    bool IsRecording { get; }

    /// <summary>記録操作または状態確認の通信中かどうかを取得します。</summary>
    bool IsBusy { get; }

    /// <summary>操作失敗後の最新状態を確認できていないかどうかを取得します。</summary>
    bool IsStateUnconfirmed { get; }

    /// <summary>操作欄に表示する記録を取得します。</summary>
    TimeRecord? DisplayedRecord { get; }

    /// <summary>記録対象のプロジェクトを選択します。</summary>
    /// <param name="project">選択するプロジェクト。未指定の場合はnull。</param>
    void SelectProject(Project? project);

    /// <summary>記録一覧を読み込みます。</summary>
    /// <returns>読み込みの完了。</returns>
    Task LoadAsync();

    /// <summary>記録を開始します。</summary>
    /// <returns>開始操作の完了。</returns>
    Task StartTimeRecordAsync();

    /// <summary>記録を終了します。</summary>
    /// <returns>終了操作の完了。</returns>
    Task EndTimeRecordAsync();
}
