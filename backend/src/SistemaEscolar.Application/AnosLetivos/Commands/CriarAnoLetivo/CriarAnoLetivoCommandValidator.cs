using FluentValidation;

namespace SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain, não aqui.
/// </summary>
public sealed class CriarAnoLetivoCommandValidator : AbstractValidator<CriarAnoLetivoCommand>
{
    public CriarAnoLetivoCommandValidator()
    {
        RuleFor(c => c.Ano)
            .InclusiveBetween(2000, 2100).WithMessage("Ano letivo deve estar entre 2000 e 2100.");

        RuleFor(c => c.DataFim)
            .GreaterThan(c => c.DataInicio).WithMessage("Data de fim deve ser posterior à data de início.");
    }
}
