using System.Text.RegularExpressions;
using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.SharedKernel.ValueObjects;

public sealed partial class Email : ValueObject
{
    public string Endereco { get; }

    private Email(string endereco)
    {
        Endereco = endereco;
    }

    public static Result<Email> Criar(string enderecoInformado)
    {
        var endereco = (enderecoInformado ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(endereco) || !RegexEmail().IsMatch(endereco))
            return Result.Falha<Email>("E-mail inválido.");

        return Result.Ok(new Email(endereco));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Endereco;
    }

    public override string ToString() => Endereco;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex RegexEmail();
}
