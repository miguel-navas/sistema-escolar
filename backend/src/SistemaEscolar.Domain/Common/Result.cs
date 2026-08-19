namespace SistemaEscolar.Domain.Common;

/// <summary>
/// Resultado de uma operação que pode falhar por regra de negócio.
/// Preferir isso a lançar exceção para erros de validação de domínio
/// esperados (exceção fica reservada para estado realmente inválido/bug).
/// </summary>
public class Result
{
    public bool Sucesso { get; }
    public string? Erro { get; }

    protected Result(bool sucesso, string? erro)
    {
        if (sucesso && erro is not null)
            throw new InvalidOperationException("Resultado de sucesso não pode ter mensagem de erro.");
        if (!sucesso && erro is null)
            throw new InvalidOperationException("Resultado de falha precisa de mensagem de erro.");

        Sucesso = sucesso;
        Erro = erro;
    }

    public static Result Ok() => new(true, null);
    public static Result Falha(string erro) => new(false, erro);

    public static Result<T> Ok<T>(T valor) => new(valor, true, null);
    public static Result<T> Falha<T>(string erro) => new(default, false, erro);
}

public class Result<T> : Result
{
    public T? Valor { get; }

    protected internal Result(T? valor, bool sucesso, string? erro) : base(sucesso, erro)
    {
        Valor = valor;
    }
}
