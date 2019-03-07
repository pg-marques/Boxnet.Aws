using Boxnet.Aws.Infra.Core.Json;
using Boxnet.Aws.Model.Aws.Iam.Roles;
using System;

namespace Boxnet.Aws.Infra.Aws.Iam.Roles
{
    public class IamRoleIdConverter : GuidEntityIdConverter<IamRoleId>
    {
        protected override IamRoleId CreateEntityId(Guid value)
        {
            return new IamRoleId(value);
        }
    }
}
