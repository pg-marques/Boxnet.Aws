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
            var roles = Enumerable.Empty<IamRole>();
            var filter = new ResourceNameContainsCaseInsensitiveFilter("Morpheus");

            var oldRepository = new IamRolesJsonFileRepository(@"C:\Users\paul.marques\Desktop\InfraApp\SummerProd\Old_IamRoles.json");
            var newRepository = new IamRolesJsonFileRepository(@"C:\Users\paul.marques\Desktop\InfraApp\SummerProd\New_IamRoles.json");

            using (var boxnetIamRolesService = new IamRolesService(boxnetAwsAccessKeyId, boxnetAwsAccessKey, defaultAwsEndpointRegion))
                roles = await boxnetIamRolesService.ListByFilterAsync(filter);

            foreach (var role in roles)
                await oldRepository.AddAsync(role);

            //Handling
            var stackPrefix = "SummerProd";            
            var newRoles = new List<IamRole>();

            foreach (var role in roles)
            {
                var id = new IamRoleId(string.Format("{0}{1}", stackPrefix, role.Id.Name));
                id.AddAlias(role.Id.Name);
                id.AddAlias(role.Id.Arn);

                newRoles.Add(
                    new IamRole(
                        id, 
                        role.Path, 
                        role.Description, 
                        role.MaxSessionDuration, 
                        role.AssumeRolePolicyDocument));
            }

            foreach (var role in newRoles)
                await newRepository.AddAsync(role);

            //Restore            
            using (var infraAppIamRolesService = new IamRolesService(infraAppAccessKeyId, infraAppAccessKey, defaultAwsEndpointRegion))
            {
                foreach (var role in newRoles)
                {
                    await infraAppIamRolesService.CreateAsync(role);
                    await newRepository.SaveAsync(role);
                }

                foreach (var role in newRoles)
                    await infraAppIamRolesService.DeleteAsync(role);
            }
        }
    }
}
