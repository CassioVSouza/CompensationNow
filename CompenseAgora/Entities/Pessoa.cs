namespace CompenseAgora.Entities;

public class Pessoa
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Sobrenome { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CognitoSub { get; set; } = string.Empty;
    public string? Endereco { get; set; }
    public string? Bairro { get; set; }
    public string? Numero { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Pais { get; set; }
    public string? Celular { get; set; }

    public ICollection<Viagem> Viagens { get; set; } = new List<Viagem>();
    public ICollection<Energia> Energias { get; set; } = new List<Energia>();
}
