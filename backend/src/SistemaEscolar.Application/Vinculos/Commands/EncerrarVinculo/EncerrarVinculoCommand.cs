using MediatR;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Application.Vinculos.DTOs;

namespace SistemaEscolar.Application.Vinculos.Commands.EncerrarVinculo;

public sealed record EncerrarVinculoCommand(Guid VinculoId) : IRequest<Result<VinculoProfessorDisciplinaTurmaDto>>;
