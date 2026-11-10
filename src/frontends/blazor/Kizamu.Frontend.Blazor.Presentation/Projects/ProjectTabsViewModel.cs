using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Presentation.Projects;

/// <summary>プロジェクト登録画面の状態と操作を管理します。</summary>
public sealed class ProjectTabsViewModel
{
    readonly List<Project> projects = [];

    /// <summary>登録したプロジェクトを取得します。</summary>
    public IReadOnlyList<Project> Projects => projects;

    /// <summary>選択したプロジェクトを取得します。</summary>
    public Project? SelectedProject { get; private set; }

    /// <summary>表示するプロジェクトを選択します。</summary>
    public void SelectProject(Project project) => SelectedProject = project;

    /// <summary>登録ダイアログを表示するかどうかを取得します。</summary>
    public bool ShowProjectDialog { get; private set; }

    /// <summary>登録するプロジェクト名を取得または設定します。</summary>
    public string ProjectName { get; set; } = "";

    /// <summary>登録ダイアログを開きます。</summary>
    public void OpenProjectDialog() => ShowProjectDialog = true;

    /// <summary>登録せずにダイアログを閉じます。</summary>
    public void CancelProjectDialog() => ShowProjectDialog = false;

    /// <summary>入力した名前を登録してダイアログを閉じます。</summary>
    public void RegisterProject()
    {
        projects.Add(new(ProjectName));
        ProjectName = "";
        ShowProjectDialog = false;
    }
}
