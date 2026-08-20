using MediatR;
using SistemaEscolar.Application.Professores.Commands.CriarProfessor;
using SistemaEscolar.Application.Professores.DTOs;

namespace SistemaEscolar.Application.Professores.Queries.ObterProfessorPorId;

public sealed record ObterProfessorPorIdQuery(Guid ProfessorId) : IRequest<Result<ProfessorDto>>;
