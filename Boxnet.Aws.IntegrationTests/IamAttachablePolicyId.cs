using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamAttachablePolicyId : ResourceId<IamAttachablePolicyId>
    {
        public IamAttachablePolicyId(string name) : base(name) { }

        public IamAttachablePolicyId(string name, string arn) : base(name, arn) { }

        public IamAttachablePolicyId(Guid guid, string name) : base(guid, name) { }

        public IamAttachablePolicyId(Guid guid, string name, string arn) : base(guid, name, arn) { }
    }
}
