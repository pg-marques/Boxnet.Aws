using Boxnet.Aws.Mvp.Lambdas;
using Boxnet.Aws.Mvp.S3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.Mvp.IntegrationTests.S3
{
    [TestClass]
    public class S3Tests
    {
        private readonly string boxnetAwsAccessKeyId = Environment.GetEnvironmentVariable("BoxnetAwsAccessKeyId");
        private readonly string boxnetAwsAccessKey = Environment.GetEnvironmentVariable("BoxnetAwsAccessKey");
        private readonly string infraAppAccessKeyId = Environment.GetEnvironmentVariable("InfraAppAccessKeyId");
        private readonly string infraAppAccessKey = Environment.GetEnvironmentVariable("InfraAppAccessKey");
        private readonly string defaultAwsEndpointRegion = Environment.GetEnvironmentVariable("DefaultAwsEndpointRegion");

        private const string StackName = "Summer";
        private const string StackEnvironment = "Homolog";
        private const string FilterName = "Morpheus";
        private const string FilterLambdaSpecialName = "GetPhotoFacebook";
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
            using (var service = new S3BucketsService(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion))
            {
                await service.CopyAsync(new ResourceNamePrefixInsensitiveCaseFilter(FilterName));
            }
        }
    }
}
