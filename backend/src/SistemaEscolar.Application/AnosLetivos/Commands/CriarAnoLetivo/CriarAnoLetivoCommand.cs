using MediatR;
using SistemaEscolar.Application.AnosLetivos.DTOs;

namespace SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;

/// <summary>
/// Comando = intenção do usuário. Não contém lógica, só dados de entrada.
/// </summary>
public sealed record CriarAnoLetivoCommand(
    int Ano,
    DateOnly DataInicio,
    DateOnly DataFim
) : IRequest<Result<AnoLetivoDto>>;

/// <summary>
/// Envelope simples de resultado para a camada de Application (distinto do
/// Result do Domain, para não vazar tipo de domínio até a Api).
/// </summary>
public sealed record Result<T>(bool Sucesso, T? Valor, string? Erro)
{
    public static Result<T> Ok(T valor) => new(true, valor, null);
    public static Result<T> Falha(string erro) => new(false, default, erro);
}
