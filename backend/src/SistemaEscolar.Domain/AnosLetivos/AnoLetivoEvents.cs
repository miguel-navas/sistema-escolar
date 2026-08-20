using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.AnosLetivos;

/// <summary>
/// Disparado quando um ano letivo é criado (sempre como Planejado).
/// </summary>
public sealed record AnoLetivoCriadoEvent(Guid AnoLetivoId, int Ano, DateTime OcorridoEm) : IDomainEvent;

/// <summary>
/// Disparado quando um ano letivo passa a ser o corrente. Outros contextos
/// (ex: relatórios, painel) podem reagir para saber qual é o ano letivo vigente.
/// </summary>
public sealed record AnoLetivoAtivadoEvent(Guid AnoLetivoId, DateTime OcorridoEm) : IDomainEvent;

/// <summary>
/// Disparado quando um ano letivo é fechado. Pode ser ouvido pelo contexto
/// Acadêmico para disparar cálculos de situação final (Etapa 5).
/// </summary>
public sealed record AnoLetivoEncerradoEvent(Guid AnoLetivoId, DateTime OcorridoEm) : IDomainEvent;
