using Boxnet.Aws.Infra.Core.Json;
using Boxnet.Aws.Model.Aws.Iam.Users;
using System;

namespace Boxnet.Aws.Infra.Aws.Iam.Users
{
    public class IamUserIdConverter : GuidEntityIdConverter<IamUserId>
    {
        protected override IamUserId CreateEntityId(Guid value)
        {
            return new IamUserId(value);
        }
    }
}
