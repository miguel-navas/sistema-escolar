namespace SistemaEscolar.Domain.Common;

/// <summary>
/// Base para toda entidade do domínio. Identidade é o que define igualdade,
/// não os valores dos atributos.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id da entidade não pode ser vazio.", nameof(id));

        Id = id;
    }

    // Necessário para ORMs / serialização.
    protected Entity() { }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();
}
