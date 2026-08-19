namespace SistemaEscolar.Domain.Common;

/// <summary>
/// Raiz de agregado: único ponto de entrada para modificar o estado do
/// agregado de fora. Acumula eventos de domínio a serem publicados depois
/// que a transação for persistida com sucesso (ver Infrastructure/UnitOfWork).
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(Guid id) : base(id) { }

    protected AggregateRoot() { }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
