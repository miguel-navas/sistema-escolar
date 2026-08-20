using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.AnosEscolares;

/// <summary>
/// Disparado quando um ano escolar/série é criado.
/// </summary>
public sealed record AnoEscolarCriadoEvent(Guid AnoEscolarId, string Nome, DateTime OcorridoEm) : IDomainEvent;
