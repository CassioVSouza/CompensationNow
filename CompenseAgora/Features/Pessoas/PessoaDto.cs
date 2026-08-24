namespace CompenseAgora.Features.Pessoas;

public record PessoaDto(
    int Codigo,
    string Nome,
    string Sobrenome,
    string Email,
    string Endereco,
    string Bairro,
    string Numero,
    string Cidade,
    string Estado,
    string Pais,
    string Celular);
