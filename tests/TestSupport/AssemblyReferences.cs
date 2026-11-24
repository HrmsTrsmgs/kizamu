namespace Kizamu.Tests;

/// <summary>対象の型が属するアセンブリの直接参照を、層の依存規則で扱う種類ごとに取得します。</summary>
sealed class AssemblyReferences(Type type)
{
    readonly IEnumerable<string> names =
        from reference in type.Assembly.GetReferencedAssemblies()
        let name = reference.Name
        where name is not null
        select name!;

    /// <summary>参照しているKizamuのアセンブリ名です。</summary>
    public IEnumerable<string> Kizamu =>
        from name in names
        where name.StartsWith("Kizamu.", StringComparison.Ordinal)
        select name;

    /// <summary>HTTPクライアントとASP.NET Coreの参照先です。</summary>
    public IEnumerable<string> Http =>
        from name in names
        where name is "System.Net.Http" or "System.Net.Http.Json" || name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)
        select name;

    /// <summary>JSON処理を担う参照先です。HTTP経由のJSON拡張も含みます。</summary>
    public IEnumerable<string> Json =>
        from name in names
        where name is "System.Text.Json" or "System.Net.Http.Json"
        select name;
}
