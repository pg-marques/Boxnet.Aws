using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    [TestClass]
    public class IamRolesTests
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
            var roles = Enumerable.Empty<IamRole>();
            var filter = new ResourceNameContainsCaseInsensitiveFilter("Morpheus");

            using (var boxnetIamPoliciesService = new IamAttachablePoliciesService(boxnetAwsAccessKeyId, boxnetAwsAccessKey, defaultAwsEndpointRegion))
                policies = await boxnetIamPoliciesService.ListByFilterAsync(filter);

            using (var boxnetIamRolesService = new IamRolesService(boxnetAwsAccessKeyId, boxnetAwsAccessKey, defaultAwsEndpointRegion))
                roles = await boxnetIamRolesService.ListByFilterAsync(filter);

            //Handling
            var stackPrefix = "SummerProd";
            var newPolicies = new List<IamAttachablePolicy>();
            var newRoles = new List<IamRole>();

            foreach (var policy in policies)
            {
                var id = new IamAttachablePolicyId(string.Format("{0}{1}", stackPrefix, policy.Id.Name));
                id.AddAlias(policy.Id.Name);
                id.AddAlias(policy.Id.Arn);

                newPolicies.Add(new IamAttachablePolicy(id, policy.Description, policy.Document, policy.Path));
            }

            foreach (var role in roles)
            {
                var id = new IamRoleId(string.Format("{0}{1}", stackPrefix, role.Id.Name));
                id.AddAlias(role.Id.Name);
                id.AddAlias(role.Id.Arn);

                var newRole = new IamRole(id, role.Path, role.Description, role.MaxSessionDuration, role.AssumeRolePolicyDocument);
                foreach (var attachedPolicyId in role.AttachedPoliciesIds)
                {
                    var newPolicy = newPolicies.Select(p => p.Id).FirstOrDefault(policyId => policyId.Aliases.Any(alias => alias == attachedPolicyId.Arn));
                    newRole.AddAttachedPolicyId(newPolicy);
                }

                foreach (var policy in role.InlinePolicies)
                    newRole.AddInlinePolicy(
                        new IamInlinePolicy(
                            new IamInlinePolicyId(string.Format("{0}{1}", stackPrefix, policy.Id.Name)),
                            policy.Document));

                newRoles.Add(newRole);
            }

            //Restore
            using (var infraAppIamPoliciesService = new IamAttachablePoliciesService(infraAppAccessKeyId, infraAppAccessKey, defaultAwsEndpointRegion))
            using (var infraAppIamRolesService = new IamRolesService(infraAppAccessKeyId, infraAppAccessKey, defaultAwsEndpointRegion))
            {
                foreach (var policy in newPolicies)
                    await infraAppIamPoliciesService.CreateAsync(policy);

                foreach (var role in newRoles)
                    await infraAppIamRolesService.CreateAsync(role);

                foreach (var role in newRoles)
                    await infraAppIamRolesService.DeleteAsync(role);

                foreach (var policy in newPolicies)
                    await infraAppIamPoliciesService.DeleteAsync(policy);
            }
        }
    }
}
