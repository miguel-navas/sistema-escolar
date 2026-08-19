using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Alunos.Commands.CriarAluno;
using SistemaEscolar.Application.Alunos.Queries.ObterAlunoPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/alunos")]
public sealed class AlunosController : ControllerBase
{
    private readonly ISender _sender;

    public AlunosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarAlunoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarAlunoCommand(
            request.NomeCompleto,
            request.DataNascimento,
            request.Cpf,
            request.ResponsavelId);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterAlunoPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarAlunoRequest(
    string NomeCompleto,
    DateOnly DataNascimento,
    string? Cpf,
    Guid ResponsavelId);
