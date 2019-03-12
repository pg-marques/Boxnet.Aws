using Boxnet.Aws.Mvp.Cognito;
using Boxnet.Aws.Mvp.Iam;
using Boxnet.Aws.Mvp.Lambdas;
using Boxnet.Aws.Mvp.Newtworking;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Boxnet.Aws.Mvp.IntegrationTests.Cognito
{
    [TestClass]
    public class CognitoTests
    {
        private readonly string boxnetAwsAccessKeyId = Environment.GetEnvironmentVariable("BoxnetAwsAccessKeyId");
        private readonly string boxnetAwsAccessKey = Environment.GetEnvironmentVariable("BoxnetAwsAccessKey");
        private readonly string infraAppAccessKeyId = Environment.GetEnvironmentVariable("InfraAppAccessKeyId");
        private readonly string infraAppAccessKey = Environment.GetEnvironmentVariable("InfraAppAccessKey");
        private readonly string defaultAwsEndpointRegion = Environment.GetEnvironmentVariable("DefaultAwsEndpointRegion");

        private const string StackName = "Summer";
        private const string StackEnvironment = "Prod";
        private const string FilterName = "Morpheus";
        private const string VPCName = "REDE_BOXNET";
        private const string SubnetsPrefix = "SUB_MIDDLE_";
        private const string SecurityGroupName = "lambda-integracoes";
        private const string DirectoryPath = @"C:\Users\paul.marques\Desktop\InfraApp\Temp";

        [TestMethod]
        public async Task Test()
        {
            var filter = new ResourceNamePrefixInsensitiveCaseFilter(FilterName);
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
                await service.CopyAllRolesAsync(FilterName);
            }

            using (var service = new VpcsService(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion))
            {
                await service.CopyAllNetworkingResources(
                    new ResourceNamePrefixInsensitiveCaseFilter(VPCName),
                    new ResourceNamePrefixInsensitiveCaseFilter(SubnetsPrefix),
                    new ResourceNamePrefixInsensitiveCaseFilter(SecurityGroupName));
            }

            using (var service = new LambdasService(
                 stack,
                 boxnetAwsAccessKeyId,
                 boxnetAwsAccessKey,
                 defaultAwsEndpointRegion,
                 boxnetAwsAccessKeyId,
                 boxnetAwsAccessKey,
                 defaultAwsEndpointRegion,
                 DirectoryPath))
            {
                await service.CopyAsync(
                    filter,
                    stack.IamRoles.FirstOrDefault(item => item.Id.NewName.EndsWith("AWSLambdasMorpheus")),
                    stack.Vpcs.First().Subnets,
                    stack.Vpcs.First().SecurityGroups);
            }


            using (var service = new UsersPoolsService(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion))
            {
                await service.CopyAsync(filter);
            }
        }
    }
}
