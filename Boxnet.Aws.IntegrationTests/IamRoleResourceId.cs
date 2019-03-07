using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamRoleResourceId : ResourceId<IamRoleResourceId>
    {
        public IamRoleResourceId(string name) : base(name) { }

        public IamRoleResourceId(string name, string arn) : base(name, arn) { }
    }
}
