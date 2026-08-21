using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Professores.Commands.CriarProfessor;
using SistemaEscolar.Application.Professores.Queries.ObterProfessorPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/professores")]
public sealed class ProfessoresController : ControllerBase
{
    private readonly ISender _sender;

    public ProfessoresController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarProfessorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarProfessorCommand(request.NomeCompleto, request.Email, request.Formacao);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterProfessorPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarProfessorRequest(
    string NomeCompleto,
    string Email,
    string Formacao);
