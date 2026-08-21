using MediatR;
using SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;
using SistemaEscolar.Application.Disciplinas.DTOs;

namespace SistemaEscolar.Application.Disciplinas.Queries.ObterDisciplinaPorId;

public sealed record ObterDisciplinaPorIdQuery(Guid DisciplinaId) : IRequest<Result<DisciplinaDto>>;
