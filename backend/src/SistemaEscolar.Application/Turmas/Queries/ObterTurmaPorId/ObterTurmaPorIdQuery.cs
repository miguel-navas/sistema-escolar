using MediatR;
using SistemaEscolar.Application.Turmas.Commands.CriarTurma;
using SistemaEscolar.Application.Turmas.DTOs;

namespace SistemaEscolar.Application.Turmas.Queries.ObterTurmaPorId;

public sealed record ObterTurmaPorIdQuery(Guid TurmaId) : IRequest<Result<TurmaDto>>;
