using MediatR;
using SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;
using SistemaEscolar.Application.Matriculas.DTOs;

namespace SistemaEscolar.Application.Matriculas.Queries.ObterMatriculaPorId;

public sealed record ObterMatriculaPorIdQuery(Guid MatriculaId) : IRequest<Result<MatriculaDto>>;
