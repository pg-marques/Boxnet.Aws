using Amazon.EC2.Model;
using Boxnet.Aws.Mvp.Apis;
using Boxnet.Aws.Mvp.CloudWatch;
using Boxnet.Aws.Mvp.Iam;
using Boxnet.Aws.Mvp.Lambdas;
using Boxnet.Aws.Mvp.Newtworking;
using Boxnet.Aws.Mvp.S3;
using Boxnet.Aws.Mvp.Sns;
using Boxnet.Aws.Mvp.Sqs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.Mvp.IntegrationTests.Lambdas
{
    [TestClass]
    public class LambdasTests
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
        private const string VPCName = "REDE_BOXNET";
        private const string SubnetsPrefix = "SUB_MIDDLE_";
        private const string SecurityGroupName = "lambda-integracoes";
        private const string RolePrefix = "AWSLambdasMorpheus";
        private const string DirectoryPath = @"C:\Users\paul.marques\Desktop\InfraApp\Temp";
        private const string StatusContactEventName = "StatusContactEvent";
        private const string ReleasePrefix = "Release";
        private const string MailingPrefix = "PendingMailing";
        [TestMethod]
        public async Task TestLambdas()
        {
            var stack = new Stack()
            {
                Name = StackName,
                Environment = StackEnvironment
            };

            var filter = new ResourceNamePrefixInsensitiveCaseFilter(FilterName);

            using (var service = new IamRolesService(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion))
            {
                await service.FillStackWithExistingRoles(new ResourceNamePrefixInsensitiveCaseFilter(RolePrefix));
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
                await service.FillStackAsync(
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
                    new OrFilter(new ResourceNamePrefixInsensitiveCaseFilter(FilterName), new EqualsCaseInsensitiveFilter(FilterLambdaSpecialName)), 
                    stack.IamRoles.FirstOrDefault(item => item.Id.NewName.EndsWith("AWSLambdasMorpheus") && item.Id.NewName.StartsWith("Summer")),
                    stack.Vpcs.First().Subnets,
                    stack.Vpcs.First().SecurityGroups);
            }
        }        

        [TestMethod]
        public async Task TestLambdasPolicies()
        {
            var stack = new Stack()
            {
                Name = StackName,
                Environment = StackEnvironment
            };

            var filter = new ResourceNamePrefixInsensitiveCaseFilter(FilterName);

            //using (var service = new CloudWatchService(
            //    stack,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion))
            //{
            //    await service.CopyAsync(new ResourceNamePrefixInsensitiveCaseFilter(FilterName));
            //}

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

            //using (var service = new LambdasService(
            //    stack,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion,
            //    DirectoryPath))
            //{
            //    await service.FillStackWithLambdasOnDestinationAsync(
            //        new OrFilter(new ResourceNamePrefixInsensitiveCaseFilter(FilterName), new EqualsCaseInsensitiveFilter(FilterLambdaSpecialName)));
            //}

            //using (var service = new ApisService(
            //    stack,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion))
            //{
            //    await service.CopyAsync(filter);
            //}

            //using (var service = new SnsTopicsService(
            //    stack,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion))
            //{
            //    await service.CopyAsync(
            //        new AndFilter(
            //            new OrFilter(
            //                new ResourceNameContainsTermInsensitiveCaseFilter(FilterName),
            //                new EqualsCaseInsensitiveFilter(StatusContactEventName),
            //                new ResourceNamePrefixInsensitiveCaseFilter(ReleasePrefix),
            //                new ResourceNamePrefixInsensitiveCaseFilter(MailingPrefix)),
            //            new NotFilter(new ResourceNamePrefixInsensitiveCaseFilter(stack.Name))));
            //}

            //using (var service = new SqsQueuesService(
            //    stack,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion))
            //{
            //    await service.CopyAsync(new ResourceNamePrefixInsensitiveCaseFilter(FilterName));
            //}

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
                await service.CopyPoliciesAsync(
                    new OrFilter(new ResourceNamePrefixInsensitiveCaseFilter(FilterName), new EqualsCaseInsensitiveFilter(FilterLambdaSpecialName)));
            }
        }

        [TestMethod]
        public async Task TestLambdasPolicies2()
        {
            var stack = new Stack()
            {
                Name = StackName,
                Environment = StackEnvironment
            };

            var filter = new ResourceNamePrefixInsensitiveCaseFilter(FilterName);

            using (var service = new CloudWatchService(
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
                await service.FillStackWithLambdasOnDestinationAsync(
                    new OrFilter(new ResourceNamePrefixInsensitiveCaseFilter(FilterName), new EqualsCaseInsensitiveFilter(FilterLambdaSpecialName)));
            }

            //using (var service = new ApisService(
            //    stack,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion,
            //    boxnetAwsAccessKeyId,
            //    boxnetAwsAccessKey,
            //    defaultAwsEndpointRegion))
            //{
            //    await service.CopyAsync(filter);
            //}

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

            using (var service = new SqsQueuesService(
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
                await service.CopyVariablesAsync(
                    new OrFilter(new ResourceNamePrefixInsensitiveCaseFilter(FilterName), new EqualsCaseInsensitiveFilter(FilterLambdaSpecialName)),
                    "morpheus.ch3pscc0opdn.us-east-1.rds.amazonaws.com",
                    "summer-homolog.ch3pscc0opdn.us-east-1.rds.amazonaws.com");
            }
        }

        [TestMethod]
        public async Task FixVPCAndRole()
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
                await service.FillStackWithExistingRoles(new ResourceNamePrefixInsensitiveCaseFilter(RolePrefix));
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
                await service.FillStackAsync(
                    new ResourceNamePrefixInsensitiveCaseFilter(VPCName),
                    new ResourceNamePrefixInsensitiveCaseFilter(SubnetsPrefix),
                    new ResourceNamePrefixInsensitiveCaseFilter(SecurityGroupName));
            }

            var filter = new ResourceNamePrefixInsensitiveCaseFilter(FilterName);
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
                await service.CopyVariablesVPCsAndRoles(
                     new OrFilter(new ResourceNamePrefixInsensitiveCaseFilter(FilterName), new EqualsCaseInsensitiveFilter(FilterLambdaSpecialName)),
                    stack.IamRoles.FirstOrDefault(item => item.Id.NewName.EndsWith("AWSLambdasMorpheus") && item.Id.NewName.StartsWith("Summer")),
                    stack.Vpcs.First().Subnets,
                    new List<AwsSecurityGroup>()
                    {
                        new AwsSecurityGroup()
                        {
                            Id = new ResourceIdWithAwsId()
                            {
                                NewId = "sg-0ff8214a9634114f4"
                            },
                            Description = "summer_homolog_lambdas",
                            VpcId = stack.Vpcs.First().Id
                        }
                    });
            }

            
        }

        [TestMethod]
        public async Task TestLambdasVersions()
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
                await service.FillStackWithExistingRoles(new ResourceNamePrefixInsensitiveCaseFilter(RolePrefix));
            }

            using (var service = new CloudWatchService(
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
                await service.FillStackWithLambdasOnDestinationAsync(
                    new OrFilter(new ResourceNamePrefixInsensitiveCaseFilter(FilterName), new EqualsCaseInsensitiveFilter(FilterLambdaSpecialName)));
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
                await service.FillStackAsync(
                    new ResourceNamePrefixInsensitiveCaseFilter(VPCName),
                    new ResourceNamePrefixInsensitiveCaseFilter(SubnetsPrefix),
                    new ResourceNamePrefixInsensitiveCaseFilter(SecurityGroupName));
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
            using (var service = new SqsQueuesService(
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

            var filter = new ResourceNamePrefixInsensitiveCaseFilter(FilterName);
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
                await service.CopyVersionsAsync(
                     new OrFilter(new ResourceNamePrefixInsensitiveCaseFilter(FilterName), new EqualsCaseInsensitiveFilter(FilterLambdaSpecialName)),
                    stack.IamRoles.FirstOrDefault(item => item.Id.NewName.EndsWith("AWSLambdasMorpheus") && item.Id.NewName.StartsWith("Summer")),
                    stack.Vpcs.First().Subnets,
                    new List<AwsSecurityGroup>()
                    {
                        new AwsSecurityGroup()
                        {
                            Id = new ResourceIdWithAwsId()
                            {
                                NewId = "sg-0ff8214a9634114f4"
                            },
                            Description = "summer_homolog_lambdas",
                            VpcId = stack.Vpcs.First().Id
                        }
                    },
                    "morpheus.ch3pscc0opdn.us-east-1.rds.amazonaws.com",
                    "summer-homolog.ch3pscc0opdn.us-east-1.rds.amazonaws.com");
            }
        }

        [TestMethod]
        public async Task TestV2()
        {
            var stack = new Stack()
            {
                Name = StackName,
                Environment = StackEnvironment
            };

            using (var service = new LambdasServiceV2(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                DirectoryPath))
            {
                await service.CopyVersionsAsync();
            }
        }

        [TestMethod]
        public async Task TestV2Aliases()
        {
            var stack = new Stack()
            {
                Name = StackName,
                Environment = StackEnvironment
            };

            using (var service = new LambdasServiceV2(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                DirectoryPath))
            {
                await service.CopyAliasesAsync(
                    new OrFilter(
                        new ResourceNamePrefixInsensitiveCaseFilter(FilterName), 
                        new EqualsCaseInsensitiveFilter(FilterLambdaSpecialName)));
            }
        }
    }
}
