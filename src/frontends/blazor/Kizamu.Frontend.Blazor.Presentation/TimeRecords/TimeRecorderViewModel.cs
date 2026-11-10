using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>記録画面の一覧、操作対象、直近の操作結果を管理します。HTMLやHTTPには依存しません。</summary>
/// <param name="service">確定した作業記録を取得する操作境界。</param>
public sealed class TimeRecorderViewModel(ITimeRecordService service)
{
    List<TimeRecord> records = [];
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

    /// <summary>操作した直近の記録を取得します。終了後も結果を残し、再開始時に差し替えます。</summary>
    public TimeRecord? DisplayedRecord { get; private set; }

    /// <summary>記録操作の対象プロジェクトを選択します。nullはプロジェクト未指定を表します。</summary>
    /// <param name="project">対象プロジェクト。未指定の場合はnull。</param>
    public void SelectProject(Project? project) => selectedProject = project;

    /// <summary>一覧を読み込み、最初の記録中の作業を終了操作の対象にします。</summary>
    /// <returns>読み込みと画面状態の更新の完了。</returns>
    public async Task LoadAsync()
    {
        records = [.. await service.GetTimeRecordsAsync()];
        CurrentId =
            (from record in records
             where record.EndedAt is null
             select (Guid?)record.Id).FirstOrDefault();
    }

    /// <summary>記録を開始し、一覧と操作欄へ確定した結果を反映します。</summary>
    /// <returns>開始と画面状態の更新の完了。</returns>
    public async Task StartTimeRecordAsync()
    {
        var project = selectedProject;
        var record = await service.StartTimeRecordAsync();
        records.Add(record);
        DisplayedRecord = record;
        SetCurrentId(project, record.Id);
    }

    /// <summary>操作対象の記録を終了し、一覧と、表示中であれば操作欄を更新します。対象がなければ何もしません。</summary>
    /// <returns>終了と画面状態の更新の完了。</returns>
    public async Task EndTimeRecordAsync()
    {
        if (CurrentId is { } id)
        {
            var record = await service.EndTimeRecordAsync(id);
            records[records.FindIndex(it => it.Id == record.Id)] = record;
            if (DisplayedRecord is not null)
            {
                DisplayedRecord = record;
            }
            CurrentId = null;
        }
    }
}
