using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Presentation.Projects;

/// <summary>プロジェクト追加ダイアログの下書きと登録操作を管理します。</summary>
public sealed class ProjectRegistrationViewModel
{
    readonly Action<Project> addProject;

    /// <summary>登録が確定したプロジェクトを一覧へ渡す操作を受け取ります。</summary>
    /// <param name="addProject">登録先の一覧に追加する操作。</param>
    internal ProjectRegistrationViewModel(Action<Project> addProject) => this.addProject = addProject;

    /// <summary>追加ダイアログを表示するかどうかを取得します。</summary>
    public bool IsOpen { get; private set; }

    /// <summary>登録する名前の下書きを取得または設定します。</summary>
    public string ProjectName { get; set; } = "";

    /// <summary>下書きを保ったまま追加ダイアログを開きます。</summary>
    public void Open() => IsOpen = true;

    /// <summary>下書きを保ったまま追加ダイアログを閉じます。</summary>
    public void Cancel() => IsOpen = false;

    /// <summary>入力した名前でプロジェクトを登録し、下書きを空にしてダイアログを閉じます。</summary>
    public void Register()
    {
        addProject(new(ProjectName));
        ProjectName = "";
        IsOpen = false;
    }
}
