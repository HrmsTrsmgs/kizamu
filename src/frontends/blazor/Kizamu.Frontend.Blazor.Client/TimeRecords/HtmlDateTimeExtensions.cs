using System.Globalization;

namespace Kizamu.Frontend.Blazor.Client.TimeRecords;

static class HtmlDateTimeExtensions
{
    extension(DateTimeOffset self)
    {
        public string ToHtmlDateTimeAttribute() =>
            self.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
    }
}
