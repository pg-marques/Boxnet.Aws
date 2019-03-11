using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Boxnet.Aws.Mvp.Iam;
using Boxnet.Aws.Mvp.Newtworking;

namespace Boxnet.Aws.Mvp.Lambdas
{
    public class LambdasService : IDisposable
    {
        protected readonly AmazonLambdaClient sourceClient;
        protected readonly AmazonLambdaClient destinationClient;
        protected readonly Stack stack;
        private readonly string directoryPath;
        protected readonly Dictionary<string, string> tags;

        public LambdasService(
            Stack stack,
            string sourceAccessKey,
            string sourceSecretKey,
            string sourceRegion,
            string destinationAccessKey,
            string destinationSecretKey,
            string destinationRegion,
            string directoryPath)
        {
            this.stack = stack;
            this.directoryPath = directoryPath;
            sourceClient = new AmazonLambdaClient(new BasicAWSCredentials(sourceAccessKey, sourceSecretKey), RegionEndpoint.GetBySystemName(sourceRegion));
            destinationClient = new AmazonLambdaClient(new BasicAWSCredentials(destinationAccessKey, destinationSecretKey), RegionEndpoint.GetBySystemName(destinationRegion));
            tags = new Dictionary<string, string>()
            {
                {
                    "Project",
                    stack.Name
                },
                {
                    "Environment",
                    stack.Environment
                },
                {
                    "ProjectEnvironment",
                    string.Format("{0}{1}", stack.Name, stack.Environment)
                }
            };
        }

        public async Task CopyAsync(IResourceNameFilter filter, IamRole role, IEnumerable<AwsSubnet> subnets, IEnumerable<AwsSecurityGroup> groups)
        {
            var lambdas = await ListLambdasOnSourceAsync(filter);
            var collection = Convert(lambdas, role, subnets, groups);
            await CreateAsync(lambdas, collection);
        }

        private async Task DownloadAsync(List<FunctionConfiguration> lambdas)
        {
            foreach (var lambda in lambdas)
            {
                var urlRequest = new GetFunctionRequest()
                {
                    FunctionName = lambda.FunctionName
                };

                var urlResponse = await sourceClient.GetFunctionAsync(urlRequest);
                var url = urlResponse.Code.Location;
                using (WebClient webClient = new WebClient())
                {
                    var directory = Path.Combine(directoryPath, "lambdas");

                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    webClient.DownloadFile(url, Path.Combine(directory, string.Format("{0}.zip", lambda.FunctionName)));
                }
            }
        }

        private List<Lambda> Convert(List<FunctionConfiguration> functions, IamRole role, IEnumerable<AwsSubnet> subnets, IEnumerable<AwsSecurityGroup> groups)
        {
            return functions.Select(item => new Lambda()
            {
                DeadLetterConfig = item.DeadLetterConfig,
                Description = item.Description,
                Handler = item.Handler,
                Id = new ResourceIdWithArn()
                {
                    PreviousArn = item.FunctionArn,
                    PreviousName = item.FunctionName,
                    NewName = NewNameFor(item.FunctionName)
                },
                KMSKeyArn = item.KMSKeyArn,
                Layers = null,
                MemorySize = item.MemorySize,
                PublishOnCreation = true,
                Role = role.Id.NewArn,
                Runtime = item.Runtime,
                Timeout = item.Timeout,
                TracingConfig =
                    item.TracingConfig != null ?
                    new TracingConfig()
                    {
                        Mode = item.TracingConfig.Mode
                    } :
                    null,
                VpcConfig = new VpcConfig()
                {
                    SecurityGroupIds = groups.Select(group => group.Id.NewId).ToList(),
                    SubnetIds = subnets.Select(subnet => subnet.Id.NewId).ToList()
                }
            }).ToList();
        }

        private async Task CreateAsync(List<FunctionConfiguration> data, List<Lambda> lambdas)
        {
            var existingLambdas = await ListLambdasOnDestinationAsync();
            foreach (var lambda in lambdas)
            {
                var existingVpc = existingLambdas.FirstOrDefault(item => item.FunctionName == lambda.Id.NewName);
                if (existingVpc != null)
                    lambda.Id.NewArn = existingVpc.FunctionArn;
            }

            var pendinglambdas = lambdas.Where(vpc => string.IsNullOrWhiteSpace(vpc.Id.NewArn)).ToList();

            if (pendinglambdas == null || pendinglambdas.Count < 1)
                return;

            //await DownloadAsync(data);
            foreach (var lambda in pendinglambdas)
            {
                var filePath = Path.Combine(directoryPath, "lambdas", string.Format("{0}.zip", lambda.Id.PreviousName));
                var bytes = File.ReadAllBytes(filePath);
                var request = new CreateFunctionRequest()
                {
                    Code = new FunctionCode()
                    {
                        ZipFile = new MemoryStream(bytes),
                    },
                    DeadLetterConfig = lambda.DeadLetterConfig,
                    Description = lambda.Description,
                    Environment = lambda.Environment,
                    FunctionName = lambda.Id.NewName,
                    Handler = lambda.Handler,
                    KMSKeyArn = lambda.KMSKeyArn,
                    Layers = lambda.Layers,
                    MemorySize = lambda.MemorySize,
                    Publish = lambda.PublishOnCreation,
                    Role = lambda.Role,
                    Runtime = lambda.Runtime,
                    StreamTransferProgress = null,
                    Timeout = lambda.Timeout,
                    TracingConfig = lambda.TracingConfig,
                    VpcConfig = lambda.VpcConfig,
                    Tags = tags
                };

                var response = await destinationClient.CreateFunctionAsync(request);
                lambda.Id.NewArn = response.FunctionArn;
            }
        }
        protected string NewNameFor(string name)
        {
            return string.Format("{0}{1}", Prefix(), name);
        }

        protected string Prefix()
        {
            return string.Format("{0}{1}_", stack.Name, stack.Environment);
        }

        private async Task<List<FunctionConfiguration>> ListLambdasOnDestinationAsync()
        {
            var filter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            var collection = new List<FunctionConfiguration>();

            string marker = null;
            do
            {
                var request = new ListFunctionsRequest()
                {
                    Marker = marker
                };

                var response = await destinationClient.ListFunctionsAsync(request);

                collection.AddRange(response.Functions.Where(item => filter.IsValid(item.FunctionName)));

                marker = response.NextMarker;

            } while (marker != null);

            return collection;
        }

        private async Task<List<FunctionConfiguration>> ListLambdasOnSourceAsync(IResourceNameFilter filter)
        {
            var prefixFilter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            var collection = new List<FunctionConfiguration>();

            string marker = null;
            do
            {
                var request = new ListFunctionsRequest()
                {
                    Marker = marker
                };

                var response = await sourceClient.ListFunctionsAsync(request);

                collection.AddRange(response.Functions.Where(item => filter.IsValid(item.FunctionName) && !prefixFilter.IsValid(item.FunctionName)));

                marker = response.NextMarker;

            } while (marker != null);

            return collection;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    sourceClient.Dispose();
                    destinationClient.Dispose();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}