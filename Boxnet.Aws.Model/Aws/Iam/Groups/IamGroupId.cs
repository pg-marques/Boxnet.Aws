using Boxnet.Aws.Model.Core;
using System;

namespace Boxnet.Aws.Model.Aws.Iam.Groups
{
    public class IamGroupId : GuidEntityId
    {
        public IamGroupId() : base() { }

        public IamGroupId(Guid value) : base(value) { }
    }
}
