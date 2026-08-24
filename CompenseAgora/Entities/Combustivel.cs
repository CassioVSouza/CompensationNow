namespace CompenseAgora.Entities;

public class Combustivel
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string UnidadeMedida { get; set; } = string.Empty;

    public int? CodigoCombustivelBiogenico { get; set; }
    public Combustivel? CombustivelBiogenico { get; set; }

    public int? CodigoCombustivelFossil { get; set; }
    public Combustivel? CombustivelFossil { get; set; }

    public ICollection<FatorCombustivel> FatoresDoCombustivel { get; set; } = new List<FatorCombustivel>();
    public ICollection<Viagem> Viagens { get; set; } = new List<Viagem>();

    public ICollection<Frota> FrotasComoPrimario { get; set; } = new List<Frota>();
    public ICollection<Frota> FrotasComoBiogenico { get; set; } = new List<Frota>();
    public ICollection<Frota> FrotasComoFossil { get; set; } = new List<Frota>();
}
