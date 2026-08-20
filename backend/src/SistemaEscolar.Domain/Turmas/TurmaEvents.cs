using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Turmas;

/// <summary>
/// Disparado quando uma turma é criada — seja a primeira turma de um grupo
/// (Turma.Criar) ou uma turma-irmã aberta automaticamente por lotação
/// (Turma.AbrirTurmaIrma). Outros contextos podem reagir, ex: notificar a
/// coordenação pedagógica.
/// </summary>
public sealed record TurmaCriadaEvent(Guid TurmaId, string Nome, DateTime OcorridoEm) : IDomainEvent;

/// <summary>
/// Disparado quando a turma atinge o número máximo de vagas ao ocupar a
/// última vaga disponível.
/// </summary>
public sealed record TurmaLotadaEvent(Guid TurmaId, DateTime OcorridoEm) : IDomainEvent;
