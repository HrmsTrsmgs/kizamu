namespace Kizamu.Domain;

/// <summary>作業時間の記録先となるプロジェクトを表します。</summary>
/// <param name="name">プロジェクトの名前。</param>
public sealed class Project(string name)
{
    /// <summary>プロジェクトの名前を取得します。</summary>
    public string Name { get; } = name;
}
