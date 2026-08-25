namespace CompenseAgora.Features.Energias;

public record EnergiaDto(
    int Codigo,
    int CodigoPessoa,
    DateOnly CriadoEm,
    DateOnly DataReferencia,
    decimal Quantidade,
    decimal EmissaoCO2);
