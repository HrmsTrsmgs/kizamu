using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>記録画面で使う一覧、直近の操作結果、プロジェクトごとの終了対象を管理します。</summary>
internal sealed class TimeRecorderState
{
    List<TimeRecord> records = [];
    // 「未指定」の終了対象は、プロジェクトを選び直しても保持する。
    Guid? unspecifiedCurrentId;
    readonly Dictionary<Project, Guid?> projectCurrentIds = [];

    /// <summary>操作欄が参照するプロジェクトを取得または設定します。<see langword="null"/> は未指定を表します。</summary>
    public Project? SelectedProject { get; set; }

    /// <summary>選択中の対象で終了を要求できる記録のIDを取得します。終了対象がなければ <see langword="null"/> です。</summary>
    public Guid? CurrentId =>
        SelectedProject is null ? unspecifiedCurrentId : projectCurrentIds.GetValueOrDefault(SelectedProject);

    /// <summary>サーバーの応答順を保ち、取得中に確定した操作を反映した一覧を取得します。</summary>
    public IReadOnlyList<TimeRecord> Records => records;

    /// <summary>操作欄に表示する直近の記録を取得します。一覧の取得だけでは新しい表示対象を選びません。</summary>
    public TimeRecord? DisplayedRecord { get; private set; }

    /// <summary>一覧と各タブの終了対象を更新し、操作欄の記録が一覧にあれば同じIDの最新値を反映します。</summary>
    /// <param name="loadedRecords">サーバーから応答順で取得した一覧。</param>
    /// <param name="updatesDuringLoad">一覧の応答を待つ間に確定した操作結果を、確定した順に並べたもの。</param>
    public void ApplyLoadedRecords(IReadOnlyList<TimeRecord> loadedRecords, IReadOnlyList<TimeRecord> updatesDuringLoad)
    {
        records = MergeLoadedRecords(loadedRecords, updatesDuringLoad);
        DisplayedRecord = records.FirstOrDefault(record => record.Id == DisplayedRecord?.Id) ?? DisplayedRecord;
        var currentId = FindCurrentIdAfterLoad(updatesDuringLoad);
        ClearUnavailableCurrentIds();
        SetCurrentId(SelectedProject, currentId);
    }

    /// <summary>開始した記録を一覧の末尾と操作欄に反映し、開始元の終了対象にします。</summary>
    /// <param name="project">開始要求時のプロジェクト。未指定の場合は <see langword="null"/>。</param>
    /// <param name="record">開始要求の応答で確定した記録。</param>
    public void ApplyStartedRecord(Project? project, TimeRecord record)
    {
        records.Add(record);
        DisplayedRecord = record;
        SetCurrentId(project, record.Id);
    }

    /// <summary>一覧の同じIDの記録を置き換え、要求元の終了対象を解除します。操作欄に記録がある場合は終了結果を表示します。</summary>
    /// <param name="project">終了要求時のプロジェクト。未指定の場合は <see langword="null"/>。</param>
    /// <param name="record">終了要求の応答で確定した記録。対応するIDが一覧にあることを前提とします。</param>
    public void ApplyEndedRecord(Project? project, TimeRecord record)
    {
        records[records.FindIndex(it => it.Id == record.Id)] = record;
        if (DisplayedRecord is not null)
        {
            DisplayedRecord = record;
        }
        SetCurrentId(project, null);
    }

    /// <summary>要求元の終了対象を書き換えます。未指定の割当はプロジェクト別の辞書から独立して保持します。</summary>
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

    /// <summary>タブを切り替えても古い終了対象が復活しないよう、合成後の一覧で未終了と確認できない割当を全タブで解除します。</summary>
    void ClearUnavailableCurrentIds()
    {
        var activeIds =
            (from record in records
             where record.EndedAt is null
             select record.Id).ToHashSet();
        if (unspecifiedCurrentId is { } id && !activeIds.Contains(id)) unspecifiedCurrentId = null;

        var unavailableProjects =
            from assignment in projectCurrentIds
            where assignment.Value is { } currentId && !activeIds.Contains(currentId)
            select assignment.Key;

        foreach (var project in unavailableProjects.ToArray())
        {
            projectCurrentIds[project] = null;
        }
    }

    /// <summary>一覧取得中に操作した終了対象を優先し、それ以外は、選択中の対象に割り当てられる最初の未終了記録を選びます。</summary>
    Guid? FindCurrentIdAfterLoad(IReadOnlyList<TimeRecord> updatesDuringLoad)
    {
        var currentId = CurrentId;
        if (updatesDuringLoad.Any(record => record.Id == currentId && record.EndedAt is null))
        {
            return currentId;
        }

        var availableRecords =
            from record in records
            where record.EndedAt is null
            where record.Id == currentId ||
                (record.Id != unspecifiedCurrentId && !projectCurrentIds.ContainsValue(record.Id))
            select record;

        return availableRecords.FirstOrDefault()?.Id;
    }
}
