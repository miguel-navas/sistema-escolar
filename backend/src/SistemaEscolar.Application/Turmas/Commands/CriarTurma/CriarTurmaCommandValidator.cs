using FluentValidation;

namespace SistemaEscolar.Application.Turmas.Commands.CriarTurma;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain, não aqui.
/// </summary>
public sealed class CriarTurmaCommandValidator : AbstractValidator<CriarTurmaCommand>
{
    public CriarTurmaCommandValidator()
    {
        RuleFor(c => c.NomeBase)
            .NotEmpty().WithMessage("Nome base da turma é obrigatório.")
            .MaximumLength(100);

        RuleFor(c => c.Turno)
            .IsInEnum().WithMessage("Turno inválido.");

        RuleFor(c => c.AnoLetivoId)
            .NotEmpty().WithMessage("Ano letivo é obrigatório.");

        RuleFor(c => c.AnoEscolarId)
            .NotEmpty().WithMessage("Ano escolar é obrigatório.");

        RuleFor(c => c.VagasMaximas)
            .GreaterThan(0).WithMessage("Número de vagas máximas deve ser maior que zero.");
    }
}
