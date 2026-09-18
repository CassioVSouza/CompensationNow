namespace CompenseAgora.Features.FatoresComposicaoCombustivel;

public record FatorComposicaoCombustivelDto(
    int Codigo,
    int Mes,
    int Ano,
    decimal PercentualEtanol,
    decimal PercentualBiodiesel);
