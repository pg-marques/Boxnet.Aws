using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    [TestClass]
    public class IamUsersTests
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
            var groups = Enumerable.Empty<IamGroup>();
            var users = Enumerable.Empty<IamUser>();
            var filter = new ResourceNameContainsCaseInsensitiveFilter("Morpheus");

            using (var boxnetIamPoliciesService = new IamAttachablePoliciesService(boxnetAwsAccessKeyId, boxnetAwsAccessKey, defaultAwsEndpointRegion))
                policies = await boxnetIamPoliciesService.ListByFilterAsync(filter);

            using (var boxnetIamGroupsService = new IamGroupsService(boxnetAwsAccessKeyId, boxnetAwsAccessKey, defaultAwsEndpointRegion))
                groups = await boxnetIamGroupsService.ListByFilterAsync(filter);

            using (var boxnetIamUsersService = new IamUsersService(boxnetAwsAccessKeyId, boxnetAwsAccessKey, defaultAwsEndpointRegion))
                users = await boxnetIamUsersService.ListByFilterAsync(filter);

            //Handling
            var stackPrefix = "SummerProd";
            var newPolicies = new List<IamAttachablePolicy>();
            var newGroups = new List<IamGroup>();
            var newUsers = new List<IamUser>();

            foreach (var policy in policies)
            {
                var id = new IamAttachablePolicyId(string.Format("{0}_{1}", stackPrefix, policy.Id.Name));
                id.AddAlias(policy.Id.Name);
                id.AddAlias(policy.Id.Arn);

                newPolicies.Add(new IamAttachablePolicy(id, policy.Description, policy.Document, policy.Path));
            }

            foreach (var group in groups)
            {
                var id = new IamGroupId(string.Format("{0}_{1}", stackPrefix, group.Id.Name));
                id.AddAlias(group.Id.Name);
                id.AddAlias(group.Id.Arn);

                var newGroup = new IamGroup(id, group.Path);
                foreach (var policy in group.AttachedPolicies)
                {
                    var newPolicy = newPolicies.Select(p => p.Id).FirstOrDefault(policyId => policyId.Aliases.Any(alias => alias == policy.Arn));
                    newGroup.Add(newPolicy);
                }
                newGroups.Add(newGroup);
            }

            foreach (var user in users)
            {
                var id = new IamUserId(string.Format("{0}.{1}", stackPrefix, user.Id.Name));
                id.AddAlias(user.Id.Name);
                id.AddAlias(user.Id.Arn);

                var newUser = new IamUser(id, user.Path);
                foreach (var group in user.GroupsIds)
                {
                    var newGroup = newGroups.Select(p => p.Id).FirstOrDefault(groupId => groupId.Aliases.Any(alias => alias == group.Name));
                    newUser.AddGroupId(newGroup);
                }
                newUsers.Add(newUser);
            }

            //Restore
            using (var infraAppIamPoliciesService = new IamAttachablePoliciesService(infraAppAccessKeyId, infraAppAccessKey, defaultAwsEndpointRegion))
            using (var infraAppIamGroupsService = new IamGroupsService(infraAppAccessKeyId, infraAppAccessKey, defaultAwsEndpointRegion))
            using (var infraAppIamUsersService = new IamUsersService(infraAppAccessKeyId, infraAppAccessKey, defaultAwsEndpointRegion))
            {
                foreach (var policy in newPolicies)
                    await infraAppIamPoliciesService.CreateAsync(policy);

                foreach (var group in newGroups)
                    await infraAppIamGroupsService.CreateAsync(group);

                foreach (var user in newUsers)
                    await infraAppIamUsersService.CreateAsync(user);

                foreach (var user in newUsers)
                    await infraAppIamUsersService.DeleteAsync(user);

                foreach (var group in newGroups)
                    await infraAppIamGroupsService.DeleteAsync(group);

                foreach (var policy in newPolicies)
                    await infraAppIamPoliciesService.DeleteAsync(policy);
            }
        }
    }
}
