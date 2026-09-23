namespace CompenseAgora.Entities;

/// <summary>One field's before/after value inside <see cref="Auditoria.Detalhes"/>. Values are pre-formatted in pt-BR.</summary>
public record AlteracaoCampo(string Campo, string? Anterior, string? Novo);
