using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Vinculos.Commands.EncerrarVinculo;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Application.Vinculos.Queries.ObterVinculoPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/professores-disciplinas-turmas")]
public sealed class VinculosProfessorDisciplinaTurmaController : ControllerBase
{
    private readonly ISender _sender;

    public VinculosProfessorDisciplinaTurmaController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Vincular(
        [FromBody] VincularProfessorDisciplinaTurmaRequest request,
        CancellationToken cancellationToken)
    {
        var command = new VincularProfessorDisciplinaTurmaCommand(
            request.ProfessorId, request.DisciplinaId, request.TurmaId, request.AnoLetivoId);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterVinculoPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }

    [HttpPost("{id:guid}/encerrar")]
    public async Task<IActionResult> Encerrar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new EncerrarVinculoCommand(id), cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record VincularProfessorDisciplinaTurmaRequest(
    Guid ProfessorId,
    Guid DisciplinaId,
    Guid TurmaId,
    Guid AnoLetivoId);
