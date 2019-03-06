using System;
using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamGroupId : ValueObject<IamGroupId>, IResourceId
    {
        private readonly IList<string> aliases = new List<string>();

        public Guid Guid { get; }

        public string Arn { get; private set; }

        public string Name { get; }

        public IEnumerable<string> Aliases { get { return aliases; } }

        public IamGroupId(string name) : this(Guid.NewGuid(), name, null) { }

        public IamGroupId(string name, string arn) : this(Guid.NewGuid(), name, arn) { }

        public IamGroupId(Guid guid, string name) : this(guid, name, null) { }

        public IamGroupId(Guid guid, string name, string arn)
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

        protected override bool EqualsOverrided(IamGroupId other)
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
