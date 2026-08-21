using MediatR;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Application.Vinculos.DTOs;

namespace SistemaEscolar.Application.Vinculos.Queries.ObterVinculoPorId;

public sealed record ObterVinculoPorIdQuery(Guid VinculoId) : IRequest<Result<VinculoProfessorDisciplinaTurmaDto>>;
