using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamRoleId : GuidEntityId
    {
        public IamRoleId() : base() { }

        public IamRoleId(Guid value) : base(value) { }        
    }
}
