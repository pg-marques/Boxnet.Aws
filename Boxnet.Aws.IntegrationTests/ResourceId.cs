using System;
using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class ResourceId<T> : ValueObject<T>, IResourceId
        where T : ResourceId<T>
    {
        private readonly IList<string> aliases = new List<string>();

        public Guid Guid { get; }

        public string Arn { get; private set; }

        public string Name { get; }

        public IEnumerable<string> Aliases { get { return aliases; } }

        public ResourceId(string name) : this(name, null) { }

        public ResourceId(string name, string arn) : this(Guid.NewGuid(), name, arn) { }

        public ResourceId(Guid guid, string name) : this(guid, name, null) { }

        public ResourceId(Guid guid, string name, string arn)
        {
            Guid = guid;
            Name = name;
            Arn = arn;
        }

        public void AddAlias(string alias)
        {
            aliases.Add(alias);
        }

        public void SetArn(string arn)
        {
            Arn = arn;
        }

        protected override bool EqualsOverrided(T other)
        {
            return Guid.Equals(other.Guid);
        }

        protected override int GetHashCodeOverrided()
        {
            unchecked
            {
                return Guid.GetHashCode();
            }
        }
    }
}
