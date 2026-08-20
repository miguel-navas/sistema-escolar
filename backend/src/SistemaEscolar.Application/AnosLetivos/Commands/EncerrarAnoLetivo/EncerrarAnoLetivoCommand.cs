using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;

namespace SistemaEscolar.Application.AnosLetivos.Commands.EncerrarAnoLetivo;

public sealed record EncerrarAnoLetivoCommand(Guid AnoLetivoId) : IRequest<Result<AnoLetivoDto>>;
