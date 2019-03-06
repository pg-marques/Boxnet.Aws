using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamRoleId : ResourceId<IamRoleId>
    {
        public IamRoleId(string name) : base(name) { }

        public IamRoleId(string name, string arn) : base(name, arn) { }

        public IamRoleId(Guid guid, string name) : base(guid, name) { }

        public IamRoleId(Guid guid, string name, string arn) : base(guid, name, arn) { }
    }
}
