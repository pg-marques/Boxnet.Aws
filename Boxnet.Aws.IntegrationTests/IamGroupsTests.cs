using Boxnet.Aws.Infra.Aws.Iam.Groups;
using Boxnet.Aws.Model.Aws.Iam;
using Boxnet.Aws.Model.Aws.Iam.Groups;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var oldRepository = new IamGroupsJsonFileRepository(@"C:\Users\paul.marques\Desktop\InfraApp\SummerProd\Old_IamGroups.json");
            var newRepository = new IamGroupsJsonFileRepository(@"C:\Users\paul.marques\Desktop\InfraApp\SummerProd\New_IamGroups.json");

            using (var boxnetIamGroupsService = new IamGroupsService(boxnetAwsAccessKeyId, boxnetAwsAccessKey, defaultAwsEndpointRegion))
                groups = await boxnetIamGroupsService.ListByFilterAsync(filter);

            foreach (var group in groups)
                await oldRepository.AddAsync(group);

            //Handling
            var stackPrefix = "SummerProd";            
            var newGroups = new List<IamGroup>();

            foreach (var group in groups)
            {
                var resourceId = new IamGroupResourceId(string.Format("{0}_{1}", stackPrefix, group.ResourceId.Name));
                resourceId.AddAlias(group.ResourceId.Name);
                resourceId.AddAlias(group.ResourceId.Arn); 
                
                newGroups.Add(new IamGroup(new IamGroupId(), resourceId, group.Path));
            }

            foreach (var group in newGroups)
                await newRepository.AddAsync(group);

            //Restore            
            using (var infraAppIamGroupsService = new IamGroupsService(infraAppAccessKeyId, infraAppAccessKey, defaultAwsEndpointRegion))
            {
                foreach (var group in newGroups)
                {
                    await infraAppIamGroupsService.CreateAsync(group);
                    await newRepository.SaveAsync(group);
                }

                foreach (var group in newGroups)                    
                    await infraAppIamGroupsService.DeleteAsync(group);
                
            }
        }

    }
}
