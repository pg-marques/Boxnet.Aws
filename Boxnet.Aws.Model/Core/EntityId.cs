namespace Boxnet.Aws.Model.Core
{
    public abstract class EntityId<T> : ValueObject<EntityId<T>>, IEntityId
    {
        public T Value { get; }

        public EntityId(T value)
        {
            Value = value;
        }

        protected override bool EqualsOverrided(EntityId<T> other)
        {
            return Value.Equals(other.Value);
        }

        protected override int GetHashCodeOverrided()
        {
            unchecked
            {
                return Value.GetHashCode();
            }
        }
    }
}
