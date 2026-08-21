using FluentValidation;

namespace SistemaEscolar.Application.Professores.Commands.CriarProfessor;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain, não aqui.
/// </summary>
public sealed class CriarProfessorCommandValidator : AbstractValidator<CriarProfessorCommand>
{
    public CriarProfessorCommandValidator()
    {
        RuleFor(c => c.NomeCompleto)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MaximumLength(200);

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.");

        RuleFor(c => c.Formacao)
            .NotEmpty().WithMessage("Formação é obrigatória.")
            .MaximumLength(200);
    }
}
