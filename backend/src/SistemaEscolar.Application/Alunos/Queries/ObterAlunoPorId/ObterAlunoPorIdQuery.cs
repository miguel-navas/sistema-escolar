using MediatR;
using SistemaEscolar.Application.Alunos.Commands.CriarAluno;
using SistemaEscolar.Application.Alunos.DTOs;

namespace SistemaEscolar.Application.Alunos.Queries.ObterAlunoPorId;

public sealed record ObterAlunoPorIdQuery(Guid AlunoId) : IRequest<Result<AlunoDto>>;
