using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>記録画面の一覧、操作対象、直近の操作結果を管理します。HTMLやHTTPには依存しません。</summary>
/// <param name="service">確定した作業記録を取得する操作境界。</param>
public sealed class TimeRecorderViewModel(ITimeRecordService service) : ITimeRecorderViewModel
{
    readonly TimeRecorderState state = new();
    readonly List<List<TimeRecord>> pendingLoads = [];
    int operationFailureVersion;

    /// <summary>取得した順序に、新たに開始した記録を追加した一覧を取得します。</summary>
    public IReadOnlyList<TimeRecord> Records => state.Records;

    /// <summary>終了操作の対象となる記録があるかどうかを取得します。</summary>
    public bool IsRecording => state.CurrentId is not null;

    /// <summary>記録操作または状態確認の通信中かどうかを取得します。</summary>
    public bool IsBusy { get; private set; }

    /// <summary>操作失敗後の最新状態を確認できていないかどうかを取得します。</summary>
    public bool IsStateUnconfirmed { get; private set; }

    /// <inheritdoc />
    public bool CanOperate => !IsBusy && !IsStateUnconfirmed;

    /// <inheritdoc />
    public bool CanReload => !IsBusy;

    /// <summary>利用者の操作で失敗が発生したかどうかを取得します。</summary>
    public bool HasOperationFailed { get; private set; }

    /// <inheritdoc />
    public TimeRecorderNotice Notice => (IsStateUnconfirmed, HasOperationFailed) switch
    {
        (true, _) => TimeRecorderNotice.StateUnconfirmed,
        (_, true) => TimeRecorderNotice.OperationFailed,
        _ => TimeRecorderNotice.None,
    };

    /// <inheritdoc />
    public Task ToggleRecordingAsync() =>
        RunUserOperationAsync(IsRecording ? EndTimeRecordAsync : StartTimeRecordAsync);

    /// <inheritdoc />
    public Task ReloadRecordsAsync() =>
        CanReload ? RunUserOperationAsync(LoadAsync) : Task.CompletedTask;

    async Task RunUserOperationAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch
        {
            HasOperationFailed = true;
        }
    }

    /// <summary>操作した直近の記録を取得します。終了後も結果を残し、再開始時に差し替えます。</summary>
    public TimeRecord? DisplayedRecord => state.DisplayedRecord;

    /// <summary>記録操作の対象プロジェクトを選択します。nullはプロジェクト未指定を表します。</summary>
    /// <param name="project">対象プロジェクト。未指定の場合はnull。</param>
    public void SelectProject(Project? project) => state.SelectedProject = project;

    /// <summary>取得した一覧と取得中に確定した操作で表示と終了対象を更新します。</summary>
    /// <returns>読み込みと画面状態の更新の完了。</returns>
    public async Task LoadAsync()
    {
        var versionAtLoadStart = operationFailureVersion;
        var isRecovery = IsStateUnconfirmed;
        if (isRecovery)
        {
            if (IsBusy) return;
            IsBusy = true;
        }
        List<TimeRecord> updatesDuringLoad = [];
        pendingLoads.Add(updatesDuringLoad);
        try
        {
            var loadedRecords = await service.GetTimeRecordsAsync();
            if (versionAtLoadStart != operationFailureVersion) return;
            state.ApplyLoadedRecords(loadedRecords, updatesDuringLoad);
            IsStateUnconfirmed = false;
        }
        finally
        {
            pendingLoads.Remove(updatesDuringLoad);
            if (isRecovery) IsBusy = false;
        }
    }

    /// <summary>操作の成否を一覧で確認し、再取得できなければ未確認状態として操作を止めます。</summary>
    async Task ReloadAfterOperationFailureAsync()
    {
        operationFailureVersion++;
        try
        {
            await LoadAsync();
        }
        catch
        {
            IsStateUnconfirmed = true;
            throw;
        }
    }

    /// <summary>未記録の対象で記録を開始し、一覧と操作欄へ確定した結果を反映します。既に記録中の場合は何もしません。</summary>
    /// <returns>開始と画面状態の更新の完了。</returns>
    public Task StartTimeRecordAsync()
    {
        if (!CanOperate || IsRecording) return Task.CompletedTask;
        var project = state.SelectedProject;
        return RunRecordOperationAsync(service.StartTimeRecordAsync, record => state.ApplyStartedRecord(project, record));
    }

    /// <summary>操作対象の記録を終了し、一覧と、表示中であれば操作欄を更新します。対象がなければ何もしません。</summary>
    /// <returns>終了と画面状態の更新の完了。</returns>
    public Task EndTimeRecordAsync()
    {
        if (!CanOperate || state.CurrentId is not { } id) return Task.CompletedTask;
        var project = state.SelectedProject;
        return RunRecordOperationAsync(() => service.EndTimeRecordAsync(id), record => state.ApplyEndedRecord(project, record));
    }

    /// <summary>要求から状態反映まで二重操作を防ぎ、失敗した場合は一覧で結果を確認します。</summary>
    async Task RunRecordOperationAsync(Func<Task<TimeRecord>> request, Action<TimeRecord> applyRecord)
    {
        IsBusy = true;
        try
        {
            var record = await request();
            applyRecord(record);
            TrackUpdateForPendingLoads(record);
        }
        catch
        {
            await ReloadAfterOperationFailureAsync();
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>応答待ちの各一覧に、この操作で確定した記録を引き継ぎます。</summary>
    void TrackUpdateForPendingLoads(TimeRecord record)
    {
        foreach (var updates in pendingLoads)
        {
            updates.Add(record);
        }
    }
}
