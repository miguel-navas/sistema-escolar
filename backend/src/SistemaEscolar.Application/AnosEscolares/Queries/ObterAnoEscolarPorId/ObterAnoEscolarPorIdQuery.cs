using MediatR;
using SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;
using SistemaEscolar.Application.AnosEscolares.DTOs;

namespace SistemaEscolar.Application.AnosEscolares.Queries.ObterAnoEscolarPorId;

public sealed record ObterAnoEscolarPorIdQuery(Guid AnoEscolarId) : IRequest<Result<AnoEscolarDto>>;
