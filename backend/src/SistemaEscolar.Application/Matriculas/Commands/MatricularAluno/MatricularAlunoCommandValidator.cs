using FluentValidation;

namespace SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain e no
/// handler (orquestração entre agregados).
/// </summary>
public sealed class MatricularAlunoCommandValidator : AbstractValidator<MatricularAlunoCommand>
{
    public MatricularAlunoCommandValidator()
    {
        RuleFor(c => c.AlunoId)
            .NotEmpty().WithMessage("Aluno é obrigatório.");

        RuleFor(c => c.TurmaId)
            .NotEmpty().WithMessage("Turma é obrigatória.");

        RuleFor(c => c.AnoLetivoId)
            .NotEmpty().WithMessage("Ano letivo é obrigatório.");
    }
}
