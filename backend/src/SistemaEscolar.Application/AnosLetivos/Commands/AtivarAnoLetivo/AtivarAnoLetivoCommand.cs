using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;

namespace SistemaEscolar.Application.AnosLetivos.Commands.AtivarAnoLetivo;

public sealed record AtivarAnoLetivoCommand(Guid AnoLetivoId) : IRequest<Result<AnoLetivoDto>>;
