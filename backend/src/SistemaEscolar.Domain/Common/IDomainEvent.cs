namespace SistemaEscolar.Domain.Common;

/// <summary>
/// Marcador para eventos de domínio. Eventos de domínio comunicam bounded
/// contexts diferentes (ex: Acadêmico -> Financeiro) sem acoplamento direto.
/// </summary>
public interface IDomainEvent
{
    DateTime OcorridoEm { get; }
}
