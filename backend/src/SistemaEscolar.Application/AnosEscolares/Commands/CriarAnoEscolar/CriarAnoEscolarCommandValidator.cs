using FluentValidation;

namespace SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain, não aqui.
/// </summary>
public sealed class CriarAnoEscolarCommandValidator : AbstractValidator<CriarAnoEscolarCommand>
{
    public CriarAnoEscolarCommandValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome do ano escolar é obrigatório.")
            .MaximumLength(100);

        RuleFor(c => c.NivelEnsino)
            .IsInEnum().WithMessage("Nível de ensino inválido.");
    }
}
