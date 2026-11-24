using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>記録画面に表示する状態と操作を提供します。</summary>
public interface ITimeRecorderViewModel
{
    /// <summary>表示する記録一覧を取得します。</summary>
    IReadOnlyList<TimeRecord> Records { get; }

    /// <summary>記録中かどうかを取得します。</summary>
    bool IsRecording { get; }

    /// <summary>開始・終了操作を受け付ける状態かどうかを取得します。</summary>
    bool CanOperate { get; }

    /// <summary>利用者による一覧の再取得を受け付ける状態かどうかを取得します。</summary>
    bool CanReload { get; }

    /// <summary>操作欄に表示する通知を取得します。最新状態が未確認の間は再取得の案内を優先します。</summary>
    TimeRecorderNotice Notice { get; }

    /// <summary>操作欄に表示する記録を取得します。</summary>
    TimeRecord? DisplayedRecord { get; }

    /// <summary>記録対象のプロジェクトを選択します。</summary>
    /// <param name="project">選択するプロジェクト。未指定の場合はnull。</param>
    void SelectProject(Project? project);

    /// <summary>記録一覧を読み込みます。</summary>
    /// <returns>読み込みの完了。</returns>
    Task LoadAsync();

    /// <summary>記録中なら終了し、待機中なら開始します。操作の失敗は通知に反映します。</summary>
    /// <returns>記録操作と通知の更新の完了。</returns>
    Task ToggleRecordingAsync();

    /// <summary>利用者の操作として一覧を再取得し、失敗は通知に反映します。</summary>
    /// <returns>再取得と通知の更新の完了。</returns>
    Task ReloadRecordsAsync();
}
