using System;

namespace Boxnet.Aws.IntegrationTests
{
    public abstract class GuidEntityId : EntityId<Guid>
    {
        public GuidEntityId(Guid value) : base(value) { }
        public GuidEntityId() : this(Guid.NewGuid()) { }
    }
}
