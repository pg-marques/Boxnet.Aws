using Boxnet.Aws.Model.Core;
using System;

namespace Boxnet.Aws.Model.Aws.Iam.Users
{
    public class IamUserId : GuidEntityId
    {
        public IamUserId() : base() { }

        public IamUserId(Guid value) : base(value) { }
    }
}
