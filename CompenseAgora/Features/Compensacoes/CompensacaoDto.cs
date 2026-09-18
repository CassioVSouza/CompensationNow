namespace CompenseAgora.Features.Compensacoes;

public record CompensacaoDto(
    int Codigo,
    int CodigoPessoa,
    DateOnly CriadoEm,
    DateOnly DataReferencia,
    string TipoCompensacao,
    decimal QuantidadeCompensada);
