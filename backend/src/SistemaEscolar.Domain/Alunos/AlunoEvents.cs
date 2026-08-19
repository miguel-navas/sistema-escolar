using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Alunos;

/// <summary>
/// Disparado quando um aluno é cadastrado. Outros bounded contexts (ex:
/// Financeiro, para criar a cobrança de matrícula) podem reagir a este evento.
/// </summary>
public sealed record AlunoCadastradoEvent(Guid AlunoId, string NomeCompleto, DateTime OcorridoEm) : IDomainEvent;

/// <summary>
/// Disparado quando o responsável formaliza o consentimento para uso de
/// reconhecimento facial do aluno. O contexto de Frequência/Biometria escuta
/// este evento para liberar o enrolamento do PerfilBiometrico.
/// </summary>
public sealed record ConsentimentoBiometricoRegistradoEvent(Guid AlunoId, DateTime OcorridoEm) : IDomainEvent;
