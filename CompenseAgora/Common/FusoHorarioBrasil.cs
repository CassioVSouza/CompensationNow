namespace CompenseAgora.Common;

/// <summary>
/// Timestamps are stored in UTC; users see Brasília time. The IANA id resolves on Windows too (.NET 6+ with ICU).
/// </summary>
public static class FusoHorarioBrasil
{
    public static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateTime ParaHorarioLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Brasilia);

    /// <summary>UTC instant at which the given Brasília calendar day starts.</summary>
    public static DateTime InicioDoDiaEmUtc(DateOnly data) =>
        TimeZoneInfo.ConvertTimeToUtc(data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Brasilia);
}
