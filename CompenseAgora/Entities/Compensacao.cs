namespace CompenseAgora.Entities;

public class Compensacao
{
    public int Codigo { get; set; }

    public int CodigoPessoa { get; set; }
    public Pessoa Pessoa { get; set; } = null!;

    public DateOnly DataReferencia { get; set; }
    public string TipoCompensacao { get; set; } = string.Empty;
    public decimal QuantidadeCompensada { get; set; }
    public DateOnly CriadoEm { get; set; }
}
