using FluentValidation;

namespace SistemaEscolar.Application.Alunos.Commands.CriarAluno;

/// <summary>
/// Validação de FORMATO/entrada (obrigatoriedade, tamanho). Regra de
/// NEGÓCIO (ex: CPF matematicamente válido, idade plausível) fica no Domain,
/// não aqui — este validador é sobre a forma da requisição, não sobre a
/// invariante do agregado.
/// </summary>
public sealed class CriarAlunoCommandValidator : AbstractValidator<CriarAlunoCommand>
{
    public CriarAlunoCommandValidator()
    {
        RuleFor(c => c.NomeCompleto)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MaximumLength(200);

        RuleFor(c => c.ResponsavelId)
            .NotEmpty().WithMessage("Responsável é obrigatório.");

        RuleFor(c => c.DataNascimento)
            .NotEmpty().WithMessage("Data de nascimento é obrigatória.");
    }
}
