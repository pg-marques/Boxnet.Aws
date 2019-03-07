using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamGroupId : GuidEntityId
    {
        public IamGroupId() : base() { }

        public IamGroupId(Guid value) : base(value) { }
    }
}
