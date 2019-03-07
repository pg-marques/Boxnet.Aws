using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamGroupResourceId : ResourceId<IamGroupResourceId>
    {
        public IamGroupResourceId(string name) : base(name)
        {
        }

        public IamGroupResourceId(string name, string arn) : base(name, arn)
        {
        }
    }
}
