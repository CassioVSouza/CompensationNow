namespace CompenseAgora.Features.Viagens;

public record ViagemDto(
    int Codigo,
    int CodigoPessoa,
    int? CodigoFrota,
    string? FrotaNome,
    int? CodigoCombustivel,
    string? CombustivelNome,
    DateOnly CriadoEm,
    DateOnly DataReferencia,
    decimal Consumo,
    int AnoFrota,
    decimal DistanciaKM,
    decimal EmissaoCO2);
