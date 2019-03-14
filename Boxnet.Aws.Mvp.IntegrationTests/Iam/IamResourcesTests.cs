using Boxnet.Aws.Mvp.Iam;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.Mvp.IntegrationTests.Iam
{
    [TestClass]
    public class IamResourcesTests
    {
        private readonly string boxnetAwsAccessKeyId = Environment.GetEnvironmentVariable("BoxnetAwsAccessKeyId");
        private readonly string boxnetAwsAccessKey = Environment.GetEnvironmentVariable("BoxnetAwsAccessKey");
        private readonly string infraAppAccessKeyId = Environment.GetEnvironmentVariable("InfraAppAccessKeyId");
        private readonly string infraAppAccessKey = Environment.GetEnvironmentVariable("InfraAppAccessKey");
        private readonly string defaultAwsEndpointRegion = Environment.GetEnvironmentVariable("DefaultAwsEndpointRegion");

        private const string StackName = "Summer";
        private const string StackEnvironment = "Homolog";
        private const string FilterName = "SummerProd";

        [TestMethod]
        public async Task TestRolesCloning()
        {
            var stack = new Stack()
            {
                Name = StackName,
                Environment = StackEnvironment
            };

            using (var service = new IamRolesService(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion))
            {
                await service.CopyAllRolesAsync(new ResourceNamePrefixInsensitiveCaseFilter(FilterName));
            }
        }

        [TestMethod]
        public async Task TestGroupsCloning()
        {
            var stack = new Stack()
            {
                Name = StackName,
                Environment = StackEnvironment
            };

            using (var service = new IamGroupsService(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion))
            {
                await service.CopyAllGroupsAsync(FilterName);
            }
        }

        [TestMethod]
        public async Task TestPoliciesCloning()
        {
            var stack = new Stack()
            {
                Name = StackName,
                Environment = StackEnvironment
            };

            using (var service = new IamPoliciesService(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion))
            {
                await service.CopyAllPoliciesAsync(FilterName);
            }
        }

        //[TestMethod]
        //public async Task TestUsersCloning()
        //{
        //    var stack = new Stack()
        //    {
        //        Name = StackName,
        //        Environment = StackEnvironment
        //    };

        //    using (var service = new IamUsersService(
        //        stack,
        //        boxnetAwsAccessKeyId,
        //        boxnetAwsAccessKey,
        //        defaultAwsEndpointRegion,
        //        infraAppAccessKeyId,
        //        infraAppAccessKey,
        //        defaultAwsEndpointRegion))
        //    {
        //        await service.CopyAllUsersAsync(FilterName);
        //    }
        //}
    }
}
