using Boxnet.Aws.Infra.Core.Json;
using Boxnet.Aws.Model.Aws.Iam.Policies;
using System;

namespace Boxnet.Aws.Infra.Aws.Iam.Policies
{
    public class IamAttachablePolicyIdConverter : GuidEntityIdConverter<IamAttachablePolicyId>
    {
        protected override IamAttachablePolicyId CreateEntityId(Guid value)
        {
            return new IamAttachablePolicyId(value);
        }
    }
}
