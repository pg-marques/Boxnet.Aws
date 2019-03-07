using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamUserId : GuidEntityId
    {
        public IamUserId() : base() { }

        public IamUserId(Guid value) : base(value) { }
    }
}
