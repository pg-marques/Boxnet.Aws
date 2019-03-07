using Boxnet.Aws.Infra.Core.Json;
using Boxnet.Aws.Model.Aws.Iam.Groups;
using System;

namespace Boxnet.Aws.Infra.Aws.Iam.Groups
{
    public class IamGroupIdConverter : GuidEntityIdConverter<IamGroupId>
    {
        protected override IamGroupId CreateEntityId(Guid value)
        {
            return new IamGroupId(value);
        }
    }
}
