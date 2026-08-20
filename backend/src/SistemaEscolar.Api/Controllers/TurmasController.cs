using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Turmas.Commands.CriarTurma;
using SistemaEscolar.Application.Turmas.Queries.ObterTurmaPorId;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/turmas")]
public sealed class TurmasController : ControllerBase
{
    private readonly ISender _sender;

    public TurmasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarTurmaRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarTurmaCommand(
            request.NomeBase,
            request.Turno,
            request.AnoLetivoId,
            request.AnoEscolarId,
            request.VagasMaximas);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterTurmaPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarTurmaRequest(
    string NomeBase,
    TurnoTurma Turno,
    Guid AnoLetivoId,
    Guid AnoEscolarId,
    int VagasMaximas);
