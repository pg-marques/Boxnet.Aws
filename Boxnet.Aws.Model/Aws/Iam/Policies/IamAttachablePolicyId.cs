using Boxnet.Aws.Model.Core;
using System;

namespace Boxnet.Aws.Model.Aws.Iam.Policies
{
    public class IamAttachablePolicyId : GuidEntityId
    {
        public IamAttachablePolicyId() : base() { }

        public IamAttachablePolicyId(Guid value) : base(value) { }
    }
}
