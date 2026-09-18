namespace CompenseAgora.Features.Frotas;

public record FrotaDto(
    int Codigo,
    string Nome,
    int CodigoCombustivelPrimario,
    string? NomeCombustivelPrimario = null,
    int? CodigoCombustivelBiogenico = null,
    string? NomeCombustivelBiogenico = null,
    int? CodigoCombustivelFossil = null,
    string? NomeCombustivelFossil = null);
