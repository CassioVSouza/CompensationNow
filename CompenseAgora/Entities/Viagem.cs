namespace CompenseAgora.Entities;

public class Viagem
{
    public int Codigo { get; set; }

    public int CodigoPessoa { get; set; }
    public Pessoa Pessoa { get; set; } = null!;

    public int CodigoFrota { get; set; }
    public Frota Frota { get; set; } = null!;

    public int CodigoCombustivel { get; set; }
    public Combustivel Combustivel { get; set; } = null!;

    public DateOnly CriadoEm { get; set; }
    public DateOnly DataReferencia { get; set; }
    public decimal Consumo { get; set; }
    public int AnoFrota { get; set; }
    public decimal DistanciaKM { get; set; }
    public decimal EmissaoCO2 { get; set; }
}
