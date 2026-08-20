using FluentValidation;

namespace SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain, não aqui.
/// </summary>
public sealed class CriarDisciplinaCommandValidator : AbstractValidator<CriarDisciplinaCommand>
{
    public CriarDisciplinaCommandValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome da disciplina é obrigatório.")
            .MaximumLength(100);

        RuleFor(c => c.CargaHoraria)
            .GreaterThan(0).WithMessage("Carga horária deve ser maior que zero.");

        RuleFor(c => c.AnoEscolarId)
            .NotEmpty().WithMessage("Ano escolar é obrigatório.");
    }
}
