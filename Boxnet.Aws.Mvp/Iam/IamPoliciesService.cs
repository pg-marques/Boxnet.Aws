using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Iam
{
    public class IamPoliciesService : IamService
    {
        public IamPoliciesService(Stack stack, string sourceAccessKey, string sourceSecretKey, string sourceRegion, string destinationAccessKey, string destinationSecretKey, string destinationRegion) : base(stack, sourceAccessKey, sourceSecretKey, sourceRegion, destinationAccessKey, destinationSecretKey, destinationRegion)
        {
        }
    }
}
