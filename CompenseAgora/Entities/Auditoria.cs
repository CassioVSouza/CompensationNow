namespace CompenseAgora.Entities;

public class Auditoria
{
    public int Codigo { get; set; }

    public int CodigoPessoa { get; set; }
    public Pessoa Pessoa { get; set; } = null!;

    /// <summary>When the action happened, in UTC.</summary>
    public DateTime DataHora { get; set; }
    public TipoAcaoAuditoria Acao { get; set; }

    /// <summary>CLR name of the affected entity (e.g. "Viagem"); null for filter actions.</summary>
    public string? Entidade { get; set; }
    public int? CodigoRegistro { get; set; }

    /// <summary>Screen where a filter was applied (e.g. "Painel"); null for CRUD actions.</summary>
    public string? Tela { get; set; }
    public DateOnly? DataInicioFiltro { get; set; }
    public DateOnly? DataFimFiltro { get; set; }

    /// <summary>JSON array of <see cref="AlteracaoCampo"/> for CRUD actions.</summary>
    public string? Detalhes { get; set; }
}
