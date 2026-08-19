using MediatR;
using SistemaEscolar.Application.Alunos.DTOs;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.SharedKernel.ValueObjects;

namespace SistemaEscolar.Application.Alunos.Commands.CriarAluno;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas:
/// 1) monta Value Objects, 2) chama a fábrica do agregado, 3) persiste,
/// 4) mapeia para DTO. Toda regra de negócio real está dentro de Aluno.cs
/// e Cpf.cs (Domain).
/// </summary>
public sealed class CriarAlunoCommandHandler
    : IRequestHandler<CriarAlunoCommand, Result<AlunoDto>>
{
    private readonly IAlunoRepository _alunoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarAlunoCommandHandler(IAlunoRepository alunoRepository, IUnitOfWork unitOfWork)
    {
        _alunoRepository = alunoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AlunoDto>> Handle(CriarAlunoCommand request, CancellationToken cancellationToken)
    {
        Cpf? cpf = null;

        if (!string.IsNullOrWhiteSpace(request.Cpf))
        {
            var cpfResult = Cpf.Criar(request.Cpf);
            if (!cpfResult.Sucesso)
                return Result<AlunoDto>.Falha(cpfResult.Erro!);

            cpf = cpfResult.Valor;

            if (await _alunoRepository.ExisteComCpfAsync(cpf!.Numero, cancellationToken))
                return Result<AlunoDto>.Falha("Já existe um aluno cadastrado com este CPF.");
        }

        var alunoResult = Aluno.Cadastrar(
            request.NomeCompleto,
            request.DataNascimento,
            cpf,
            request.ResponsavelId);

        if (!alunoResult.Sucesso)
            return Result<AlunoDto>.Falha(alunoResult.Erro!);

        var aluno = alunoResult.Valor!;

        await _alunoRepository.AdicionarAsync(aluno, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        // Nota: eventos de domínio (ex: AlunoCadastradoEvent) devem ser
        // publicados aqui pela Infrastructure após o SaveChanges confirmar
        // a transação (ver DependencyInjection.cs / interceptor do DbContext).

        return Result<AlunoDto>.Ok(new AlunoDto(
            aluno.Id,
            aluno.NomeCompleto,
            aluno.DataNascimento,
            aluno.Cpf?.Formatado(),
            aluno.ResponsavelId,
            aluno.Status.ToString(),
            aluno.ConsentimentoBiometricoRegistrado));
    }
}
