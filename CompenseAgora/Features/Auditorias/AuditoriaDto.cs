using CompenseAgora.Entities;

namespace CompenseAgora.Features.Auditorias;

public record AuditoriaDto(
    int Codigo,
    DateTime DataHora,
    TipoAcaoAuditoria Acao,
    string Descricao,
    IReadOnlyList<AlteracaoCampo> Alteracoes);

public record AuditoriasPaginadasDto(IReadOnlyList<AuditoriaDto> Itens, int Total);
