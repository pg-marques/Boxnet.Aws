using Boxnet.Aws.Infra.Aws.Iam.Users;
using Boxnet.Aws.Model.Aws.Iam;
using Boxnet.Aws.Model.Aws.Iam.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
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
                var resourceId = new IamUserResourceId(string.Format("{0}.{1}", stackPrefix, user.ResourceId.Name));
                resourceId.AddAlias(user.ResourceId.Name);
                resourceId.AddAlias(user.ResourceId.Arn);

                newUsers.Add(new IamUser(new IamUserId(), resourceId, user.Path));
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
