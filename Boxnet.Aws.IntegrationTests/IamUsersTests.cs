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
            var users = Enumerable.Empty<IamUser>();
            var filter = new ResourceNameContainsCaseInsensitiveFilter("Morpheus");

            var oldRepository = new IamUsersJsonFileRepository(@"C:\Users\paul.marques\Desktop\InfraApp\SummerProd\Old_IamUsers.json");
            var newRepository = new IamUsersJsonFileRepository(@"C:\Users\paul.marques\Desktop\InfraApp\SummerProd\New_IamUsers.json");

            using (var boxnetIamUsersService = new IamUsersService(boxnetAwsAccessKeyId, boxnetAwsAccessKey, defaultAwsEndpointRegion))
                users = await boxnetIamUsersService.ListByFilterAsync(filter);

            foreach (var user in users)
                await oldRepository.AddAsync(user);

            //Handling
            var stackPrefix = "SummerProd";            
            var newUsers = new List<IamUser>();
            
            foreach (var user in users)
            {
                var id = new IamUserId(string.Format("{0}.{1}", stackPrefix, user.Id.Name));
                id.AddAlias(user.Id.Name);
                id.AddAlias(user.Id.Arn);

                newUsers.Add(new IamUser(id, user.Path));
            }

            foreach (var user in newUsers)
                await newRepository.AddAsync(user);

            //Restore
            using (var infraAppIamUsersService = new IamUsersService(infraAppAccessKeyId, infraAppAccessKey, defaultAwsEndpointRegion))
            {
                foreach (var user in newUsers)
                {
                    await infraAppIamUsersService.CreateAsync(user);
                    await newRepository.SaveAsync(user);
                }

                foreach (var user in newUsers)
                    await infraAppIamUsersService.DeleteAsync(user);
            }
        }
    }
}
