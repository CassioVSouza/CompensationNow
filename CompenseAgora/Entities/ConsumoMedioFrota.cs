namespace CompenseAgora.Entities;

public class ConsumoMedioFrota
{
    public int Codigo { get; set; }

    public int CodigoFrota { get; set; }
    public Frota Frota { get; set; } = null!;

    public bool ParaTodos { get; set; }
    public int Ano { get; set; }
    public decimal Consumo { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty;
}
