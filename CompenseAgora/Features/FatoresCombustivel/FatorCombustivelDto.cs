namespace CompenseAgora.Features.FatoresCombustivel;

public record FatorCombustivelDto(
    int Codigo,
    int CodigoCombustivel,
    string NomeCombustivel,
    int Ano,
    decimal PoderCalorificoInferior,
    decimal Densidade,
    decimal CO2,
    decimal CH4,
    decimal N2O);
