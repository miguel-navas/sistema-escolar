namespace SistemaEscolar.Domain.Alunos;

public enum StatusAluno
{
    PreCadastrado = 1,   // cadastro criado, ainda sem consentimento biométrico
    Ativo = 2,           // matriculado e apto a ter presença registrada
    Inativo = 3,         // ex: transferido, evadido
    Trancado = 4
}
