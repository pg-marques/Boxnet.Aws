using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamAttachablePolicyId : GuidEntityId
    {
        public IamAttachablePolicyId() : base() { }

        public IamAttachablePolicyId(Guid value) : base(value) { }
    }
}
