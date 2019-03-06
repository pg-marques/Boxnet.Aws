using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamGroupId : ResourceId<IamGroupId>
    {
        public IamGroupId(string name) : base(name) { }

        public IamGroupId(string name, string arn) : base(name, arn) { }

        public IamGroupId(Guid guid, string name) : base(guid, name) { }

        public IamGroupId(Guid guid, string name, string arn) : base(guid, name, arn) { }
    }
}
