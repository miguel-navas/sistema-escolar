using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;

namespace SistemaEscolar.Application.AnosLetivos.Queries.ObterAnoLetivoPorId;

public sealed record ObterAnoLetivoPorIdQuery(Guid AnoLetivoId) : IRequest<Result<AnoLetivoDto>>;
