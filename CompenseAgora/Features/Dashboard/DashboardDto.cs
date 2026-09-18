namespace CompenseAgora.Features.Dashboard;

public record MonthlyEmissionDto(int Year, int Month, decimal ViagemEmissao, decimal EnergiaEmissao, decimal Compensacao)
{
    public decimal Total => ViagemEmissao + EnergiaEmissao;
}

public record DashboardDto(
    List<MonthlyEmissionDto> MonthlyEmissions,
    decimal TotalEmissao,
    decimal TotalViagem,
    decimal TotalEnergia,
    decimal TotalCompensacao);
