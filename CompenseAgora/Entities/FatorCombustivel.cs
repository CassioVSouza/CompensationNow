namespace CompenseAgora.Entities;

public class FatorCombustivel
{
    public int Codigo { get; set; }

    public int CodigoCombustivel { get; set; }
    public Combustivel Combustivel { get; set; } = null!;

    public int Ano { get; set; }
    public decimal PoderCalorificoInferior { get; set; }
    public decimal Densidade { get; set; }
    public decimal CO2 { get; set; }
    public decimal CH4 { get; set; }
    public decimal N2O { get; set; }
}
