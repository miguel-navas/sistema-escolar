using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.AnosLetivos.Commands.AtivarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.Commands.EncerrarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.Queries.ObterAnoLetivoPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/anos-letivos")]
public sealed class AnosLetivosController : ControllerBase
{
    private readonly ISender _sender;

    public AnosLetivosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarAnoLetivoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarAnoLetivoCommand(request.Ano, request.DataInicio, request.DataFim);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterAnoLetivoPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }

    [HttpPost("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new AtivarAnoLetivoCommand(id), cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }

    [HttpPost("{id:guid}/encerrar")]
    public async Task<IActionResult> Encerrar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new EncerrarAnoLetivoCommand(id), cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarAnoLetivoRequest(
    int Ano,
    DateOnly DataInicio,
    DateOnly DataFim);
