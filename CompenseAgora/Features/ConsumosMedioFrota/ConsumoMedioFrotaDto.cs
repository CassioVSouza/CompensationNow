namespace CompenseAgora.Features.ConsumosMedioFrota;

public record ConsumoMedioFrotaDto(
    int Codigo,
    int CodigoFrota,
    string NomeFrota,
    bool ParaTodos,
    int Ano,
    decimal Consumo,
    string UnidadeMedida);
