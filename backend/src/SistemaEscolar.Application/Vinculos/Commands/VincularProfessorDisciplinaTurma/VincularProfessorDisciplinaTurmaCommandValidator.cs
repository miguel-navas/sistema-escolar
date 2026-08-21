using FluentValidation;

namespace SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain e no
/// handler (orquestração entre agregados).
/// </summary>
public sealed class VincularProfessorDisciplinaTurmaCommandValidator : AbstractValidator<VincularProfessorDisciplinaTurmaCommand>
{
    public VincularProfessorDisciplinaTurmaCommandValidator()
    {
        RuleFor(c => c.ProfessorId)
            .NotEmpty().WithMessage("Professor é obrigatório.");

        RuleFor(c => c.DisciplinaId)
            .NotEmpty().WithMessage("Disciplina é obrigatória.");

        RuleFor(c => c.TurmaId)
            .NotEmpty().WithMessage("Turma é obrigatória.");

        RuleFor(c => c.AnoLetivoId)
            .NotEmpty().WithMessage("Ano letivo é obrigatório.");
    }
}
