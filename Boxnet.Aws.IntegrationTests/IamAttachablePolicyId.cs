using System;
using System.Collections.Generic;
using System.Linq;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamAttachablePolicyId : ValueObject<IamAttachablePolicyId>, IResourceId
    {
        private readonly IList<string> aliases = new List<string>();

        public Guid Guid { get; }

        public string Arn { get; private set; }

        public string Name { get; }

        public IEnumerable<string> Aliases { get { return aliases; } }

        public IamAttachablePolicyId(string name) : this(Guid.NewGuid(), name, null) { }

        public IamAttachablePolicyId(string name, string arn) : this(Guid.NewGuid(), name, arn) { }

        public IamAttachablePolicyId(Guid id, string name) : this(id, name, null) { }

        public IamAttachablePolicyId(Guid guid, string name, string arn)
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

        protected override bool EqualsOverrided(IamAttachablePolicyId other)
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
