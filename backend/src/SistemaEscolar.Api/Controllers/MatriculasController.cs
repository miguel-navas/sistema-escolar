using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;
using SistemaEscolar.Application.Matriculas.Queries.ObterMatriculaPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/matriculas")]
public sealed class MatriculasController : ControllerBase
{
    private readonly ISender _sender;

    public MatriculasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Matricular(
        [FromBody] MatricularAlunoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new MatricularAlunoCommand(
            request.AlunoId,
            request.TurmaId,
            request.AnoLetivoId);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterMatriculaPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record MatricularAlunoRequest(
    Guid AlunoId,
    Guid TurmaId,
    Guid AnoLetivoId);
