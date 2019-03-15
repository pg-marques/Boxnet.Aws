using Boxnet.Aws.Mvp.Lambdas;
using Boxnet.Aws.Mvp.Sns;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.Mvp.IntegrationTests.Sns
{
    [TestClass]
    public class SnsTopicsTests
    {
        private readonly string boxnetAwsAccessKeyId = Environment.GetEnvironmentVariable("BoxnetAwsAccessKeyId");
        private readonly string boxnetAwsAccessKey = Environment.GetEnvironmentVariable("BoxnetAwsAccessKey");
        private readonly string infraAppAccessKeyId = Environment.GetEnvironmentVariable("InfraAppAccessKeyId");
        private readonly string infraAppAccessKey = Environment.GetEnvironmentVariable("InfraAppAccessKey");
        private readonly string defaultAwsEndpointRegion = Environment.GetEnvironmentVariable("DefaultAwsEndpointRegion");

        private const string StackName = "Summer";
        private const string StackEnvironment = "Prod";
        private const string FilterName = "Morpheus";
        private const string FilterLambdaSpecialName = "GetPhotoFacebook";
        private const string DirectoryPath = @"C:\Users\paul.marques\Desktop\InfraApp\Temp";
        private const string StatusContactEventName = "StatusContactEvent";
        private const string ReleasePrefix = "Release";
        private const string MailingPrefix = "PendingMailing";

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

            using (var service = new SnsTopicsService(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion))
            {
                await service.CopyAsync(
                    new AndFilter(
                        new OrFilter(
                            new ResourceNameContainsTermInsensitiveCaseFilter(FilterName),
                            new EqualsCaseInsensitiveFilter(StatusContactEventName),
                            new ResourceNamePrefixInsensitiveCaseFilter(ReleasePrefix),
                            new ResourceNamePrefixInsensitiveCaseFilter(MailingPrefix)),
                        new NotFilter(new ResourceNamePrefixInsensitiveCaseFilter(stack.Name))));
            }
        }
    }
}
