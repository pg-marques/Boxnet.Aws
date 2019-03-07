namespace Boxnet.Aws.Model.Core
{
    public abstract class Entity<TEntityId>
        where TEntityId : IEntityId
    {
        public TEntityId Id { get; }

        public Entity(TEntityId id)
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            var entity = obj as Entity<TEntityId>;

            if (ReferenceEquals(entity, null))
                return false;

            if (GetType() != obj.GetType())
                return false;

            return Id.Equals(entity.Id);
        }


        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Entity<TEntityId> a, Entity<TEntityId> b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
                return true;

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(Entity<TEntityId> a, Entity<TEntityId> b)
        {
            return !(a == b);
        }
    }
}
