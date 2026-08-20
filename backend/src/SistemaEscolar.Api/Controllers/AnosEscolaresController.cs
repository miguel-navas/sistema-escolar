using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;
using SistemaEscolar.Application.AnosEscolares.Queries.ObterAnoEscolarPorId;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/anos-escolares")]
public sealed class AnosEscolaresController : ControllerBase
{
    private readonly ISender _sender;

    public AnosEscolaresController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarAnoEscolarRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarAnoEscolarCommand(request.Nome, request.NivelEnsino);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterAnoEscolarPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarAnoEscolarRequest(
    string Nome,
    NivelEnsino NivelEnsino);
