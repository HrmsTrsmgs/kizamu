using Kizamu.Domain;
using Riok.Mapperly.Abstractions;

namespace Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;

/// <summary>HTTP応答の記録を、画面が扱うDomainの記録に復元します。</summary>
[Mapper]
static partial class TimeRecordResponseMapper
{
    /// <summary>サーバーで確定した識別子と時刻から記録を復元します。</summary>
    internal static partial TimeRecord ToDomain(TimeRecordResponse response);
}
