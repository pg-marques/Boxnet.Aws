using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    [TestClass]
    public class IamAttachablePoliciesTests
    {
        private readonly string boxnetAwsAccessKeyId = Environment.GetEnvironmentVariable("BoxnetAwsAccessKeyId");
        private readonly string boxnetAwsAccessKey = Environment.GetEnvironmentVariable("BoxnetAwsAccessKey");
        private readonly string infraAppAccessKeyId = Environment.GetEnvironmentVariable("InfraAppAccessKeyId");
        private readonly string infraAppAccessKey = Environment.GetEnvironmentVariable("InfraAppAccessKey");
        private readonly string defaultAwsEndpointRegion = Environment.GetEnvironmentVariable("DefaultAwsEndpointRegion");

        [TestMethod]
        public async Task BasicSnapshotAndRestoreTests()
        {            
            //Snapshot
            var policies = Enumerable.Empty<IamAttachablePolicy>();

            var oldRepository = new IamAttachablePoliciesJsonFileRepository(@"C:\Users\paul.marques\Desktop\InfraApp\SummerProd\Old_IamAttachablePolicies.json");            
            var newRepository = new IamAttachablePoliciesJsonFileRepository(@"C:\Users\paul.marques\Desktop\InfraApp\SummerProd\New_IamAttachablePolicies.json");

            using (var boxnetIamPoliciesService = new IamAttachablePoliciesService(boxnetAwsAccessKeyId, boxnetAwsAccessKey, defaultAwsEndpointRegion))
                policies = await boxnetIamPoliciesService.ListByFilterAsync(new ResourceNameContainsCaseInsensitiveFilter("Morpheus"));
            
            foreach (var policy in policies)
                await oldRepository.AddAsync(policy);

            //Handling
            var stackPrefix = "SummerProd";
            var newPolicies = new List<IamAttachablePolicy>();
            foreach (var policy in policies)
            {
                var id = new IamAttachablePolicyId(Guid.NewGuid(), string.Format("{0}{1}", stackPrefix, policy.Id.Name));
                id.AddAlias(policy.Id.Name);
                id.AddAlias(policy.Id.Arn);

                newPolicies.Add(new IamAttachablePolicy(id, policy.Description, policy.Document, policy.Path));
            }

            foreach (var policy in newPolicies)
                await newRepository.AddAsync(policy);

            //Restore
            using (var infraAppIamPoliciesService = new IamAttachablePoliciesService(infraAppAccessKeyId, infraAppAccessKey, defaultAwsEndpointRegion))
            {
                foreach (var policy in newPolicies)
                {
                    await infraAppIamPoliciesService.CreateAsync(policy);
                    await newRepository.SaveAsync(policy);
                }

                foreach (var policy in newPolicies)
                    await infraAppIamPoliciesService.DeleteAsync(policy);
            }
        }
    }
}
