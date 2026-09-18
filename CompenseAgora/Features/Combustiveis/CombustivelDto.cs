namespace CompenseAgora.Features.Combustiveis;

public record CombustivelDto(
    int Codigo,
    string Nome,
    string UnidadeMedida,
    bool CombustivelPrincipal = false,
    int? CodigoCombustivelBiogenico = null,
    string? NomeCombustivelBiogenico = null,
    int? CodigoCombustivelFossil = null,
    string? NomeCombustivelFossil = null);
