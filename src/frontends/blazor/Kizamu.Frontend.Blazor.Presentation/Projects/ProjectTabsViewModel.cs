using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Presentation.Projects;

/// <summary>プロジェクト一覧とタブの選択を管理します。</summary>
public sealed class ProjectTabsViewModel
{
    readonly List<Project> projects = [];

    /// <summary>選択されていない空のプロジェクト一覧を用意します。</summary>
    public ProjectTabsViewModel() => Registration = new(projects.Add);

    /// <summary>同じ一覧へプロジェクトを追加する登録ダイアログの状態と操作を取得します。</summary>
    public ProjectRegistrationViewModel Registration { get; }

    /// <summary>登録したプロジェクトを取得します。</summary>
    public IReadOnlyList<Project> Projects => projects;

    /// <summary>選択したプロジェクトを取得します。</summary>
    public Project? SelectedProject { get; private set; }

    /// <summary>表示するプロジェクトを選択します。</summary>
    /// <param name="project">選択するプロジェクト。未指定の場合はnull。</param>
    public void SelectProject(Project? project) => SelectedProject = project;
}
