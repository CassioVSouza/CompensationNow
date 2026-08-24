namespace CompenseAgora.Entities;

public class Frota
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;

    public int CodigoCombustivelPrimario { get; set; }
    public Combustivel CombustivelPrimario { get; set; } = null!;

    public int? CodigoCombustivelBiogenico { get; set; }
    public Combustivel? CombustivelBiogenico { get; set; }

    public int? CodigoCombustivelFossil { get; set; }
    public Combustivel? CombustivelFossil { get; set; }

    public ICollection<ConsumoMedioFrota> ConsumosMedios { get; set; } = new List<ConsumoMedioFrota>();
    public ICollection<Viagem> Viagens { get; set; } = new List<Viagem>();
}
