using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;
using SistemaEscolar.Application.Disciplinas.Queries.ObterDisciplinaPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/disciplinas")]
public sealed class DisciplinasController : ControllerBase
{
    private readonly ISender _sender;

    public DisciplinasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarDisciplinaRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarDisciplinaCommand(request.Nome, request.CargaHoraria, request.AnoEscolarId);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterDisciplinaPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarDisciplinaRequest(
    string Nome,
    int CargaHoraria,
    Guid AnoEscolarId);
