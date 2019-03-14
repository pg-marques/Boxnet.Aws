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
        private const string StackEnvironment = "Homolog";
        private const string FilterLambdaSpecialName = "GetPhotoFacebook";
        private const string FilterName = "Morpheus";
        private const string RolePrefix = "AWSLambdasMorpheus";
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

            using (var service = new IamRolesService(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion))
            {                
                await service.CopyAllRolesAsync(
                    new AndFilter(
                        new ResourceNameContainsTermInsensitiveCaseFilter(FilterName),
                        new NotFilter(new ResourceNamePrefixInsensitiveCaseFilter(string.Format("{0}", StackName)))));
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
                await service.FillStackWithLambdasOnDestinationAsync(new OrFilter(new ResourceNamePrefixInsensitiveCaseFilter(FilterName), new EqualsCaseInsensitiveFilter(FilterLambdaSpecialName)));
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
