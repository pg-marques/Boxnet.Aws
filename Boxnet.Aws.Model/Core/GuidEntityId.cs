using System;

namespace Boxnet.Aws.Model.Core
{
    public abstract class GuidEntityId : EntityId<Guid>
    {
        public GuidEntityId(Guid value) : base(value) { }
        public GuidEntityId() : this(Guid.NewGuid()) { }
    }
}
