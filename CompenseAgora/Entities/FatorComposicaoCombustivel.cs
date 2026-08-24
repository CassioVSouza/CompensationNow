namespace CompenseAgora.Entities;

public class FatorComposicaoCombustivel
{
    public int Codigo { get; set; }
    public int Mes { get; set; }
    public int Ano { get; set; }
    public decimal PercentualEtanol { get; set; }
    public decimal PercentualBiodiesel { get; set; }
}
