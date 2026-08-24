namespace CompenseAgora.Entities;

public class GasEfeitoEstufa
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Familia { get; set; } = string.Empty;
    public decimal GWP { get; set; }
}
