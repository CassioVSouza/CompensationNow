namespace CompenseAgora.Entities;

public class Energia
{
    public int Codigo { get; set; }

    public int CodigoPessoa { get; set; }
    public Pessoa Pessoa { get; set; } = null!;

    public DateOnly DataReferencia { get; set; }
    public decimal Quantidade { get; set; }
    public DateOnly CriadoEm { get; set; }
    public decimal EmissaoCO2 { get; set; }
}
