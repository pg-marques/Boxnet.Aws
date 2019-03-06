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
    public class IamGroupsTests
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
            var groups = Enumerable.Empty<IamGroup>();
            var filter = new ResourceNameContainsCaseInsensitiveFilter("Morpheus");

            using (var boxnetIamGroupsService = new IamGroupsService(boxnetAwsAccessKeyId, boxnetAwsAccessKey, defaultAwsEndpointRegion))
                groups = await boxnetIamGroupsService.ListByFilterAsync(filter);

            //Handling
            var stackPrefix = "SummerProd";            
            var newGroups = new List<IamGroup>();

            foreach (var group in groups)
            {
                var id = new IamGroupId(string.Format("{0}{1}", stackPrefix, group.Id.Name));
                id.AddAlias(group.Id.Name);
                id.AddAlias(group.Id.Arn); 
                
                newGroups.Add(new IamGroup(id, group.Path));
            }

            //Restore            
            using (var infraAppIamGroupsService = new IamGroupsService(infraAppAccessKeyId, infraAppAccessKey, defaultAwsEndpointRegion))
            {
                foreach (var group in newGroups)
                    await infraAppIamGroupsService.CreateAsync(group);

                foreach (var group in newGroups)                    
                    await infraAppIamGroupsService.DeleteAsync(group);
                
            }
        }

    }
}
