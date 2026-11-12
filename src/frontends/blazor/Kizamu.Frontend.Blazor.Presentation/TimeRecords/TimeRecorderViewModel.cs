using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>記録画面の一覧、操作対象、直近の操作結果を管理します。HTMLやHTTPには依存しません。</summary>
/// <param name="service">確定した作業記録を取得する操作境界。</param>
public sealed class TimeRecorderViewModel(ITimeRecordService service) : ITimeRecorderViewModel
{
    List<TimeRecord> records = [];
    readonly List<List<TimeRecord>> pendingLoads = [];
    Project? selectedProject;
    Guid? unspecifiedCurrentId;
    readonly Dictionary<Project, Guid?> projectCurrentIds = [];

    Guid? CurrentId
    {
        get => selectedProject is null ? unspecifiedCurrentId : projectCurrentIds.GetValueOrDefault(selectedProject);
        set => SetCurrentId(selectedProject, value);
    }

    void SetCurrentId(Project? project, Guid? id)
    {
        if (project is null)
        {
            unspecifiedCurrentId = id;
        }
        else
        {
            projectCurrentIds[project] = id;
        }
    }

    /// <summary>取得した順序に、新たに開始した記録を追加した一覧を取得します。</summary>
    public IReadOnlyList<TimeRecord> Records => records;

    /// <summary>終了操作の対象となる記録があるかどうかを取得します。</summary>
    public bool IsRecording => CurrentId is not null;

    /// <summary>記録操作または状態確認の通信中かどうかを取得します。</summary>
    public bool IsBusy { get; private set; }

    /// <summary>操作失敗後の最新状態を確認できていないかどうかを取得します。</summary>
    public bool IsStateUnconfirmed { get; private set; }

    /// <inheritdoc />
    public bool CanOperate => !IsBusy && !IsStateUnconfirmed;

    /// <inheritdoc />
    public bool HasOperationFailed { get; private set; }

    /// <inheritdoc />
    public TimeRecorderNotice Notice => (IsStateUnconfirmed, HasOperationFailed) switch
    {
        (true, _) => TimeRecorderNotice.StateUnconfirmed,
        (_, true) => TimeRecorderNotice.OperationFailed,
        _ => TimeRecorderNotice.None,
    };

    /// <inheritdoc />
    public async Task RunUserOperationAsync(Func<Task> operation)
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
    public TimeRecord? DisplayedRecord { get; private set; }

    /// <summary>記録操作の対象プロジェクトを選択します。nullはプロジェクト未指定を表します。</summary>
    /// <param name="project">対象プロジェクト。未指定の場合はnull。</param>
    public void SelectProject(Project? project) => selectedProject = project;

    /// <summary>一覧取得中の操作結果に、応答時の終了対象と同じIDの未終了記録があればその対象を保ちます。それ以外は一覧で最初の未終了記録を選びます。</summary>
    /// <returns>読み込みと画面状態の更新の完了。</returns>
    public async Task LoadAsync()
    {
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
            records = MergeLoadedRecords(loadedRecords, updatesDuringLoad);
            CurrentId = FindCurrentIdAfterLoad(updatesDuringLoad);
            IsStateUnconfirmed = false;
        }
        finally
        {
            pendingLoads.Remove(updatesDuringLoad);
            if (isRecovery) IsBusy = false;
        }
    }

    /// <summary>応答の順序を保って同じIDを最新の操作結果に置き換え、応答にないIDは最初に操作した順に加えます。</summary>
    static List<TimeRecord> MergeLoadedRecords(IReadOnlyList<TimeRecord> loadedRecords, IReadOnlyList<TimeRecord> updatesDuringLoad)
    {
        var recordsFromResponse =
            from record in loadedRecords
            select updatesDuringLoad.LastOrDefault(update => update.Id == record.Id) ?? record;

        var recordsMissingFromResponse =
            from update in updatesDuringLoad
            group update by update.Id into updates
            where !loadedRecords.Any(record => record.Id == updates.Key)
            select updates.Last();

        return [.. recordsFromResponse, .. recordsMissingFromResponse];
    }

    /// <summary>一覧取得中に操作した終了対象を優先し、それ以外は一覧の先頭の記録中項目を選びます。</summary>
    Guid? FindCurrentIdAfterLoad(IReadOnlyList<TimeRecord> updatesDuringLoad)
    {
        var currentId = CurrentId;
        return updatesDuringLoad.Any(record => record.Id == currentId && record.EndedAt is null)
            ? currentId
            : records.FirstOrDefault(record => record.EndedAt is null)?.Id;
    }

    /// <summary>操作の成否を一覧で確認し、再取得できなければ未確認状態として操作を止めます。</summary>
    async Task ReloadAfterOperationFailureAsync()
    {
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

    /// <summary>記録を開始し、一覧と操作欄へ確定した結果を反映します。</summary>
    /// <returns>開始と画面状態の更新の完了。</returns>
    public Task StartTimeRecordAsync()
    {
        if (!CanOperate) return Task.CompletedTask;
        var project = selectedProject;
        return RunRecordOperationAsync(service.StartTimeRecordAsync, record =>
        {
            records.Add(record);
            TrackUpdateForPendingLoads(record);
            DisplayedRecord = record;
            SetCurrentId(project, record.Id);
        });
    }

    /// <summary>操作対象の記録を終了し、一覧と、表示中であれば操作欄を更新します。対象がなければ何もしません。</summary>
    /// <returns>終了と画面状態の更新の完了。</returns>
    public Task EndTimeRecordAsync()
    {
        if (!CanOperate || CurrentId is not { } id) return Task.CompletedTask;
        return RunRecordOperationAsync(() => service.EndTimeRecordAsync(id), record =>
        {
            records[records.FindIndex(it => it.Id == record.Id)] = record;
            TrackUpdateForPendingLoads(record);
            if (DisplayedRecord is not null)
            {
                DisplayedRecord = record;
            }
            CurrentId = null;
        });
    }

    /// <summary>要求から状態反映まで二重操作を防ぎ、失敗した場合は一覧で結果を確認します。</summary>
    async Task RunRecordOperationAsync(Func<Task<TimeRecord>> request, Action<TimeRecord> applyRecord)
    {
        IsBusy = true;
        try
        {
            applyRecord(await request());
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
