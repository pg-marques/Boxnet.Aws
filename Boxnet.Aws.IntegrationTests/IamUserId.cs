using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamUserId : ResourceId<IamUserId>
    {
        public IamUserId(string name) : base(name) { }

        public IamUserId(string name, string arn) : base(name, arn) { }

        public IamUserId(Guid guid, string name) : base(guid, name) { }

        public IamUserId(Guid guid, string name, string arn) : base(guid, name, arn) { }
    }
}
