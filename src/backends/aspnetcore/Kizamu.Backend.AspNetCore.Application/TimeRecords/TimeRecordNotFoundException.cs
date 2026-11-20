namespace Kizamu.Backend.AspNetCore.Application.TimeRecords;

/// <summary>
/// 終了を要求した作業記録が保存先に存在しないことを示します。
/// </summary>
public sealed class TimeRecordNotFoundException : Exception;
