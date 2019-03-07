using Boxnet.Aws.Model.Core;
using System;

namespace Boxnet.Aws.Model.Aws.Iam.Roles
{
    public class IamRoleId : GuidEntityId
    {
        public IamRoleId() : base() { }

        public IamRoleId(Guid value) : base(value) { }
    }
}
