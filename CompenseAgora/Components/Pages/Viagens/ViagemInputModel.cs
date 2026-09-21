namespace CompenseAgora.Components.Pages.Viagens;

public class ViagemInputModel
{
    public int? CodigoFrota { get; set; }
    public DateOnly DataReferencia { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public TipoRegistroViagem TipoRegistro { get; set; } = TipoRegistroViagem.PorDistancia;
    public int AnoFrota { get; set; }
    public decimal Consumo { get; set; }
    public decimal DistanciaKM { get; set; }
    public int CodigoCombustivel { get; set; }
}
