namespace CompenseAgora.Entities;

public class Pessoa
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;

    public ICollection<Viagem> Viagens { get; set; } = new List<Viagem>();
    public ICollection<Energia> Energias { get; set; } = new List<Energia>();
}
