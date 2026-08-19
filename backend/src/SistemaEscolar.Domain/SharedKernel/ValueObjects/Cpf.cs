using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.SharedKernel.ValueObjects;

/// <summary>
/// Value Object para CPF. Garante, no próprio construtor, que nunca existe
/// um CPF inválido dentro do domínio (validação sempre ligada, não opcional).
/// </summary>
public sealed class Cpf : ValueObject
{
    public string Numero { get; }

    private Cpf(string numero)
    {
        Numero = numero;
    }

    public static Result<Cpf> Criar(string numeroInformado)
    {
        var digitos = new string((numeroInformado ?? string.Empty)
            .Where(char.IsDigit)
            .ToArray());

        if (digitos.Length != 11)
            return Result.Falha<Cpf>("CPF deve conter 11 dígitos.");

        if (!EhValido(digitos))
            return Result.Falha<Cpf>("CPF inválido.");

        return Result.Ok(new Cpf(digitos));
    }

    private static bool EhValido(string cpf)
    {
        if (cpf.Distinct().Count() == 1) return false; // 111.111.111-11 etc.

        int[] multiplicador1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCpf = cpf[..9];
        var soma = tempCpf.Select((c, i) => (c - '0') * multiplicador1[i]).Sum();
        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;

        tempCpf += digito1;
        soma = tempCpf.Select((c, i) => (c - '0') * multiplicador2[i]).Sum();
        resto = soma % 11;
        var digito2 = resto < 2 ? 0 : 11 - resto;

        return cpf.EndsWith($"{digito1}{digito2}");
    }

    public string Formatado() =>
        $"{Numero[..3]}.{Numero[3..6]}.{Numero[6..9]}-{Numero[9..]}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Numero;
    }

    public override string ToString() => Formatado();
}
