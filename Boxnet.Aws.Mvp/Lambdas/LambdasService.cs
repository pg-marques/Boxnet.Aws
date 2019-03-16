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
using Amazon.Auth.AccessControlPolicy;
using Newtonsoft.Json;
using A = Amazon.Lambda.Model;

namespace Boxnet.Aws.Mvp.Lambdas
{

    #region V2
    public class LambdaAliasesService
    {
        protected readonly AmazonLambdaClient client;

        public LambdaAliasesService(AmazonLambdaClient client)
        {
            this.client = client;
        }

        public async Task<List<AliasConfiguration>> ListAllAliasesOfLambdaAsync(string name)
        {
            var aliases = new List<AliasConfiguration>();
            string marker = null;
            do
            {
                var request = new ListAliasesRequest()
                {
                    FunctionName = name,
                    Marker = marker
                };

                var response = await client.ListAliasesAsync(request);

                aliases.AddRange(response.Aliases);

                marker = response.NextMarker;
            } while (marker != null);

            return aliases;
        }
    }

    public class LambdaEnvironmentVariablesService
    {
        protected readonly AmazonLambdaClient client;

        public LambdaEnvironmentVariablesService(AmazonLambdaClient client)
        {
            this.client = client;
        }
        public async Task<Dictionary<string, string>> ListAllVariablesOfLambdaAsync(string name)
        {
            return await ListAllVariablesOfLambdaVersionAsync(name, null);
        }

        public async Task<Dictionary<string, string>> ListAllVariablesOfLambdaVersionAsync(string name, string qualifier)
        {
            var request = new GetFunctionConfigurationRequest()
            {
                FunctionName = name,
                Qualifier = qualifier
            };

            var response = await client.GetFunctionConfigurationAsync(request);

            if (response?.Environment?.Variables != null)
                return response.Environment.Variables;

            return new Dictionary<string, string>();
        }

        public async Task ReplaceVariables(string name, Dictionary<string, string> variables)
        {
            var currentVariables = await GetUpdatedEnvironmentVariablesAsync(name, variables);

            var request = new UpdateFunctionConfigurationRequest()
            {
                FunctionName = name,
                Environment = new A.Environment()
                {
                    Variables = currentVariables
                }
            };

            var response = await client.UpdateFunctionConfigurationAsync(request);
        }

        private async Task<Dictionary<string, string>> GetUpdatedEnvironmentVariablesAsync(string name, Dictionary<string, string> variables)
        {
            var currentVariables = await ListAllVariablesOfLambdaAsync(name);

            if (currentVariables == null)
                currentVariables = new Dictionary<string, string>();

            foreach (var variable in variables)
            {
                if (currentVariables.ContainsKey(variable.Key))
                    currentVariables[variable.Key] = variable.Value;
                else
                    currentVariables.Add(variable.Key, variable.Value);
            }

            return currentVariables;
        }
    }

    public class LambdaVersionPublisher
    {
        protected readonly AmazonLambdaClient client;

        public LambdaVersionPublisher(AmazonLambdaClient client)
        {
            this.client = client;
        }

        public async Task<string> PublishAsync(string name, string description)
        {
            var request = new PublishVersionRequest()
            {
                FunctionName = name,
                Description = description
            };

            var response = await client.PublishVersionAsync(request);
            await Task.Delay(150);

            if (!string.IsNullOrWhiteSpace(response.Version))
                return response.Version;

            return null;
        }
    }

    public class LambdaCodeDownloaderService
    {
        protected readonly AmazonLambdaClient client;

        public LambdaCodeDownloaderService(AmazonLambdaClient client)
        {
            this.client = client;
        }

        public async Task DownloadCodeAsync(string lambdaName, string filePath)
        {
            var urlRequest = new GetFunctionRequest()
            {
                FunctionName = lambdaName
            };

            var urlResponse = await client.GetFunctionAsync(urlRequest);
            var url = urlResponse.Code.Location;

            if (string.IsNullOrWhiteSpace(urlResponse?.Code?.Location))
                return;

            using (WebClient webClient = new WebClient())
            {
                var info = new FileInfo(filePath);

                var directory = info.Directory.FullName;

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await webClient.DownloadFileTaskAsync(url, filePath);
            }
        }
    }

    public class LambdaCodeUploaderService
    {
        protected readonly AmazonLambdaClient client;

        public LambdaCodeUploaderService(AmazonLambdaClient client)
        {
            this.client = client;
        }

        public async Task UploadAsync(string filePath, string name)
        {
            var buffer = BytesFromFile(filePath);
            var request = new UpdateFunctionCodeRequest()
            {
                FunctionName = name,
                Publish = false,
                ZipFile = new MemoryStream(buffer),
            };

            await client.UpdateFunctionCodeAsync(request);
        }

        private byte[] BytesFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new byte[] { };

            return File.ReadAllBytes(filePath);
        }
    }

    public class LambdasServiceV2 : IDisposable
    {
        protected readonly AmazonLambdaClient sourceClient;
        protected readonly AmazonLambdaClient destinationClient;

        protected readonly LambdaEnvironmentVariablesService sourceLambdaEnvironmentVariablesService;
        protected readonly LambdaEnvironmentVariablesService destinationLambdaEnvironmentVariablesService;

        protected readonly LambdaCodeUploaderService sourceLambdaCodeUploaderService;
        protected readonly LambdaCodeUploaderService destinationLambdaCodeUploaderService;

        protected readonly LambdaVersionPublisher sourceLambdaVersionPublisher;
        protected readonly LambdaVersionPublisher destinationLambdaVersionPublisher;

        protected readonly LambdaAliasesService sourceLambdaAliasesService;
        protected readonly LambdaAliasesService destinationLambdaAliasesService;

        protected readonly Stack stack;
        private readonly string directoryPath;
        protected readonly Dictionary<string, string> tags;

        public LambdasServiceV2(
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

            sourceLambdaEnvironmentVariablesService = new LambdaEnvironmentVariablesService(sourceClient);
            destinationLambdaEnvironmentVariablesService = new LambdaEnvironmentVariablesService(destinationClient);

            sourceLambdaCodeUploaderService = new LambdaCodeUploaderService(sourceClient);
            destinationLambdaCodeUploaderService = new LambdaCodeUploaderService(destinationClient);

            sourceLambdaVersionPublisher = new LambdaVersionPublisher(sourceClient);
            destinationLambdaVersionPublisher = new LambdaVersionPublisher(destinationClient);

            sourceLambdaAliasesService = new LambdaAliasesService(sourceClient);
            destinationLambdaAliasesService = new LambdaAliasesService(destinationClient);

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

        public async Task CopyAliasesAsync(IResourceNameFilter filter)
        {
            var namesFromSource = await ListAllLambdasNamesOfSourceAsync(filter);
            var namesFromDestination = await ListAllLambdasNamesOfDestinationAsync();

            foreach (var name in namesFromSource)
            {
                try
                {
                    var previousName = namesFromSource.FirstOrDefault(it => name.Contains(it));
                    var aliases = await sourceLambdaAliasesService.ListAllAliasesOfLambdaAsync(previousName);
                    var x = 0;
                }
                catch (Exception ex)
                {
                    var x = 0;
                }
            }
        }

        public async Task<List<string>> ListAllLambdasNamesOfSourceAsync(IResourceNameFilter filter)
        {
            var names = new List<string>();
            string marker = null;
            do
            {
                var request = new ListFunctionsRequest()
                {
                    Marker = marker
                };

                var response = await sourceClient.ListFunctionsAsync(request);

                names.AddRange(response?.Functions?.Select(it => it.FunctionName).Where(it => filter.IsValid(it)).ToList());

                marker = response.NextMarker;

            } while (marker != null);

            return names;
        }

        public async Task<List<string>> ListAllLambdasNamesOfDestinationAsync()
        {
            var filter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            var names = new List<string>();
            string marker = null;
            do
            {
                var request = new ListFunctionsRequest()
                {
                    Marker = marker
                };

                var response = await destinationClient.ListFunctionsAsync(request);

                names.AddRange(response?.Functions?.Select(it => it.FunctionName).Where(it => filter.IsValid(it)).ToList());

                marker = response.NextMarker;

            } while (marker != null);

            return names;
        }

        public async Task CopyVersionsAsync()
        {
            const string Latest = "LATEST";
            const string VersionMap = "versions-map";

            var directory = Path.Combine(directoryPath, "lambdas", Prefix().Trim('_'), "20190315");

            if (!Directory.Exists(directory))
                return;

            var dirInfo = new DirectoryInfo(directory);
            foreach (var childDir in dirInfo.GetDirectories())
            {
                var previousFunctionName = childDir.Name;
                var newFunctionName = NewNameFor(previousFunctionName);
                try
                {
                    var versionsMapPath = Path.Combine(childDir.FullName, "versions-map.json");
                    if (File.Exists(versionsMapPath))
                    {
                        var latestPath = Path.Combine(childDir.FullName, "LATEST.zip");
                        if (File.Exists(latestPath) && previousFunctionName == "MorpheusContactGroupActivateDeactivate")
                        {
                            var variablesOnDestination = await destinationLambdaEnvironmentVariablesService.ListAllVariablesOfLambdaAsync(newFunctionName);
                            if (variablesOnDestination == null || variablesOnDestination.Count() == 0)
                                variablesOnDestination.Add("temp", string.Empty);

                            foreach (var variable in variablesOnDestination.ToList())
                            {
                                variablesOnDestination[variable.Key] = string.Format("{0}_temp", variable.Value);
                            }

                            await destinationLambdaEnvironmentVariablesService.ReplaceVariables(newFunctionName, variablesOnDestination);

                            foreach (var variable in variablesOnDestination.ToList())
                            {
                                if (variable.Key == "temp")
                                    variablesOnDestination.Remove(variable.Key);
                                else
                                    variablesOnDestination[variable.Key] = variable.Value.Replace("_temp", string.Empty);
                            }

                            var versionVariables = await sourceLambdaEnvironmentVariablesService.ListAllVariablesOfLambdaAsync(previousFunctionName);
                            foreach (var variable in versionVariables.ToList())
                            {
                                if (variablesOnDestination.ContainsKey(variable.Key))
                                    versionVariables[variable.Key] = variablesOnDestination[variable.Key];
                            }

                            await destinationLambdaEnvironmentVariablesService.ReplaceVariables(newFunctionName, versionVariables);
                            await destinationLambdaCodeUploaderService.UploadAsync(latestPath, newFunctionName);
                        }

                        continue;
                    }

                    var currentVariablesOnDestination = await destinationLambdaEnvironmentVariablesService.ListAllVariablesOfLambdaAsync(newFunctionName);
                    var versions = new Dictionary<string, string>();
                    foreach (var fileInfo in childDir.GetFiles())
                    {
                        var version = Path.GetFileNameWithoutExtension(fileInfo.Name);
                        if (version != Latest && version != VersionMap)
                        {
                            if (currentVariablesOnDestination == null || currentVariablesOnDestination.Count() == 0)
                                currentVariablesOnDestination.Add("temp", string.Empty);

                            //force changes
                            foreach (var variable in currentVariablesOnDestination.ToList())
                            {
                                currentVariablesOnDestination[variable.Key] = string.Format("{0}_temp", variable.Value);
                            }

                            await destinationLambdaEnvironmentVariablesService.ReplaceVariables(newFunctionName, currentVariablesOnDestination);

                            //fix changes
                            foreach (var variable in currentVariablesOnDestination.ToList())
                            {
                                if (variable.Key == "temp")
                                    currentVariablesOnDestination.Remove(variable.Key);
                                else
                                    currentVariablesOnDestination[variable.Key] = variable.Value.Replace("_temp", string.Empty);
                            }

                            var versionVariables = await sourceLambdaEnvironmentVariablesService.ListAllVariablesOfLambdaVersionAsync(previousFunctionName, version);
                            foreach (var variable in versionVariables.ToList())
                            {
                                if (currentVariablesOnDestination.ContainsKey(variable.Key))
                                    versionVariables[variable.Key] = currentVariablesOnDestination[variable.Key];
                            }

                            await destinationLambdaEnvironmentVariablesService.ReplaceVariables(newFunctionName, versionVariables);
                            await destinationLambdaCodeUploaderService.UploadAsync(fileInfo.FullName, newFunctionName);
                            var publishedVersion = await destinationLambdaVersionPublisher.PublishAsync(newFunctionName, string.Format("Original version: {0}.", version));

                            versions.Add(version, publishedVersion);
                        }
                    }

                    var serializedVersions = JsonConvert.SerializeObject(versions);
                    var serializedVersionsAsBytes = Encoding.UTF8.GetBytes(serializedVersions);
                    File.WriteAllBytes(versionsMapPath, serializedVersionsAsBytes);
                }
                catch (Exception ex)
                {
                    var x = 0;
                }
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
    }
    #endregion
    #endregion

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
            stack.Lambdas = collection;
        }

        public async Task CopyVariablesVPCsAndRoles(IResourceNameFilter filter, IamRole role, IEnumerable<AwsSubnet> subnets, IEnumerable<AwsSecurityGroup> groups)
        {
            var lambdas = await ListLambdasOnSourceAsync(filter);
            var collection = Convert(lambdas, role, subnets, groups);
            foreach (var lambda in collection)
            {
                var request = new UpdateFunctionConfigurationRequest()
                {
                    FunctionName = lambda.Id.NewName,
                    VpcConfig = lambda.VpcConfig,
                    Role = lambda.Role
                };
                try
                {

                    var response = await destinationClient.UpdateFunctionConfigurationAsync(request);
                }
                catch (Exception ex)
                {
                    var x = 0;
                }
            }
        }

        public async Task CopyVersionsAsync(
            IResourceNameFilter filter,
            IamRole role,
            IEnumerable<AwsSubnet> subnets,
            IEnumerable<AwsSecurityGroup> groups,
            string previousConnectionString,
            string connectionString)
        {
            var lambdas = await ListLambdasOnSourceAsync(filter);
            var collection = Convert(lambdas, null, null, null);
            foreach (var item in collection)
            {
                var versions = new List<FunctionConfiguration>();
                string marker = null;
                do
                {
                    var request = new ListVersionsByFunctionRequest()
                    {
                        FunctionName = item.Id.PreviousName,
                        Marker = marker

                    };
                    var response = await sourceClient.ListVersionsByFunctionAsync(request);
                    marker = response.NextMarker;
                    versions.AddRange(response.Versions.Where(it => filter.IsValid(it.FunctionName) && it.Version != "$LATEST").ToList());
                } while (marker != null);

                var configCollection = new List<FunctionConfiguration>();
                foreach (var version in versions)
                {
                    var request = new GetFunctionRequest()
                    {
                        FunctionName = version.FunctionName,
                        Qualifier = version.Version
                    };

                    var response = await sourceClient.GetFunctionAsync(request);
                    var code = response.Code.Location;

                    configCollection.Add(response.Configuration);
                }
                if (configCollection.Count() > 0)
                {
                    var converted = Convert(configCollection, role, subnets, groups);
                    var existingCollection = await ListLambdasOnDestinationAsync();
                    var existingConverted = Convert(existingCollection, role, subnets, groups);
                    foreach (var i in existingConverted)
                    {
                        i.Id.NewArn = i.Id.PreviousArn;
                        i.Id.NewName = i.Id.PreviousName;
                    }
                    var filtered = new Dictionary<Lambda, List<Lambda>>();
                    foreach (var i in converted.Where(it => it.Version != "$LATEST").ToList())
                    {
                        var existingItem = existingConverted.FirstOrDefault(it => it.Id.NewName == i.Id.NewName);
                        if (existingItem != null && !(i.LastModifiedOnSource > new DateTime(2019, 03, 15, 0, 0, 0) || i.LastModifiedOnSource > i.LastModifiedOnDestination))
                        {
                            existingItem.Id.PreviousArn = i.Id.PreviousArn;
                            existingItem.Id.PreviousName = i.Id.PreviousName;

                            if (filtered.ContainsKey(existingItem))
                                filtered[existingItem].Add(i);
                            else
                                filtered.Add(existingItem, new List<Lambda>() { i });
                        }
                    }

                    foreach (var i in filtered)
                    {
                        foreach (var j in filtered.Values)
                        {

                        }
                        var request = new GetFunctionConfigurationRequest()
                        {
                            FunctionName = i.Key.Id.PreviousName,
                        };

                        try
                        {
                            var response = await sourceClient.GetFunctionConfigurationAsync(request);
                            if (response?.Environment?.Variables != null)
                            {
                                var variables = new Dictionary<string, string>();
                                foreach (var variable in response.Environment.Variables)
                                {
                                    var found = false;
                                    if (stack.Buckets != null)
                                    {

                                        foreach (var s3 in stack.Buckets)
                                        {
                                            if (variable.Value.Contains(s3.Id.PreviousName))
                                            {
                                                variables.Add(variable.Key, variable.Value.Replace(s3.Id.PreviousName, s3.Id.NewName));
                                                found = true;
                                                break;
                                            }
                                        }
                                        if (found)
                                            continue;
                                    }
                                    if (stack.SnsTopics != null)
                                    {
                                        foreach (var sns in stack.SnsTopics)
                                        {
                                            if (variable.Value.Contains(sns.Id.PreviousName))
                                            {
                                                variables.Add(variable.Key, variable.Value.Replace(sns.Id.PreviousName, sns.Id.NewName));
                                                found = true;
                                                break;
                                            }
                                        }

                                        if (found)
                                            continue;
                                    }
                                    if (stack.SqsQueues != null)
                                    {
                                        foreach (var sqs in stack.SqsQueues)
                                        {
                                            if (variable.Value.Contains(sqs.Id.PreviousName))
                                            {
                                                variables.Add(variable.Key, variable.Value.Replace(sqs.Id.PreviousName, sqs.Id.NewName));
                                                found = true;
                                                break;
                                            }
                                        }
                                        if (found)
                                            continue;
                                    }
                                    if (variable.Value.Contains(previousConnectionString))
                                    {
                                        variables.Add(variable.Key, variable.Value.Replace(previousConnectionString, connectionString));
                                        found = true;

                                        if (found)
                                            continue;
                                    }

                                    variables.Add(variable.Key, variable.Value);
                                    found = true;
                                    if (found)
                                        continue;

                                }

                                if (variables.Count() > 0)
                                {
                                    var xRequest = new UpdateFunctionConfigurationRequest()
                                    {
                                        FunctionName = i.Key.Id.NewName,
                                        Environment = new Amazon.Lambda.Model.Environment()
                                        {
                                            Variables = variables
                                        }
                                    };

                                    var xResponse = await destinationClient.UpdateFunctionConfigurationAsync(xRequest);
                                }

                                var directory = Path.Combine(directoryPath, "lambdas", Prefix().Trim('_'), DateTime.UtcNow.Date.ToString("yyyyMMdd"), i.Key.Id.PreviousName);

                                if (!Directory.Exists(directory))
                                    Directory.CreateDirectory(directory);

                                var file = Path.Combine(directory, string.Format("{0}.zip", "LATEST"));

                                if (File.Exists(file))
                                {
                                    var bytes = File.ReadAllBytes(file);
                                    var newRequest = new UpdateFunctionCodeRequest()
                                    {
                                        FunctionName = i.Key.Id.NewName,
                                        Publish = true,
                                        ZipFile = new MemoryStream(bytes),
                                    };

                                    var newResponse = await destinationClient.UpdateFunctionCodeAsync(newRequest);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var y = 0;
                        }

                    }
                }
            }
        }

        public async Task CopyVariablesAsync(IResourceNameFilter filter, string previousConnectionString, string connectionString)
        {
            var lambdas = await ListLambdasOnSourceAsync(filter);
            var collection = Convert(lambdas, null, null, null);
            foreach (var item in collection)
            {
                var request = new GetFunctionConfigurationRequest()
                {
                    FunctionName = item.Id.PreviousName
                };

                var response = await sourceClient.GetFunctionConfigurationAsync(request);

                if (response?.Environment != null)
                {
                    var variables = new Dictionary<string, string>();
                    foreach (var variable in response.Environment.Variables)
                    {
                        var found = false;
                        if (stack.Buckets != null)
                        {

                            foreach (var s3 in stack.Buckets)
                            {
                                if (variable.Value.Contains(s3.Id.PreviousName))
                                {
                                    variables.Add(variable.Key, variable.Value.Replace(s3.Id.PreviousName, s3.Id.NewName));
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                                continue;
                        }
                        if (stack.SnsTopics != null)
                        {
                            foreach (var sns in stack.SnsTopics)
                            {
                                if (variable.Value.Contains(sns.Id.PreviousName))
                                {
                                    variables.Add(variable.Key, variable.Value.Replace(sns.Id.PreviousName, sns.Id.NewName));
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                                continue;
                        }
                        if (stack.SqsQueues != null)
                        {
                            foreach (var sqs in stack.SqsQueues)
                            {
                                if (variable.Value.Contains(sqs.Id.PreviousName))
                                {
                                    variables.Add(variable.Key, variable.Value.Replace(sqs.Id.PreviousName, sqs.Id.NewName));
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                                continue;
                        }
                        if (variable.Value.Contains(previousConnectionString))
                        {
                            variables.Add(variable.Key, variable.Value.Replace(previousConnectionString, connectionString));
                            found = true;

                            if (found)
                                continue;
                        }

                        variables.Add(variable.Key, variable.Value);
                        found = true;
                        if (found)
                            continue;

                    }

                    if (variables.Count() > 0)
                    {
                        var xRequest = new UpdateFunctionConfigurationRequest()
                        {
                            FunctionName = item.Id.NewName,
                            Environment = new Amazon.Lambda.Model.Environment()
                            {
                                Variables = variables
                            }
                        };

                        var xResponse = await destinationClient.UpdateFunctionConfigurationAsync(xRequest);
                    }
                }
            }
        }

        public async Task CopyPoliciesAsync(IResourceNameFilter filter)
        {
            var lambdas = await ListLambdasOnSourceAsync(filter);
            var collection = Convert(lambdas, null, null, null);

            foreach (var item in collection)
            {
                string marker = null;
                do
                {
                    var request = new GetPolicyRequest()
                    {
                        FunctionName = item.Id.PreviousName
                    };

                    try
                    {

                        var response = await sourceClient.GetPolicyAsync(request);

                        var existingFunctionRequest = new GetPolicyRequest()
                        {
                            FunctionName = item.Id.NewName
                        };

                        //var existingFunctionResponse = await destinationClient.GetPolicyAsync(existingFunctionRequest);
                        var existingPolicy = string.Empty;

                        if (response?.Policy != null)
                        {
                            var policy = response.Policy.Replace(item.Id.PreviousArn, item.Id.NewArn);
                            var changed = false;

                            if (stack.Buckets != null)
                                foreach (var s3 in stack.Buckets)
                                {
                                    if (policy.Contains(s3.Id.PreviousName))
                                    {
                                        policy = policy.Replace(s3.Id.PreviousName, s3.Id.NewName);
                                        changed = true;
                                    }
                                }

                            if (changed)
                            {
                                var parsed = Policy.FromJson(policy);
                                foreach (var statement in parsed.Statements)
                                {
                                    foreach (var principal in statement.Principals)
                                    {
                                        foreach (var action in statement.Actions)
                                        {
                                            foreach (var condition in statement.Conditions)
                                            {
                                                foreach (var value in condition.Values)
                                                {
                                                    var x = new AddPermissionRequest()
                                                    {
                                                        Principal = principal.Id,
                                                        Action = action.ActionName,
                                                        FunctionName = item.Id.NewName,
                                                        SourceArn = value,
                                                        StatementId = Guid.NewGuid().ToString()
                                                    };

                                                    var xResponse = await destinationClient.AddPermissionAsync(x);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var z = 1;
                    }
                } while (marker != null);
            }
        }

        private async Task DownloadAsync(List<Lambda> lambdas)
        {
            await DownloadAsync(lambdas, false);
        }

        private async Task DownloadFromDestinationAsync(List<Lambda> lambdas, bool useVersion)
        {
            foreach (var lambda in lambdas)
            {
                var urlRequest = new GetFunctionRequest()
                {
                    FunctionName = lambda.Id.PreviousName
                };

                var urlResponse = await destinationClient.GetFunctionAsync(urlRequest);
                var url = urlResponse.Code.Location;
                using (WebClient webClient = new WebClient())
                {
                    var directory = Path.Combine(directoryPath, "lambdas", Prefix().Trim('_'), DateTime.UtcNow.Date.ToString("yyyyMMdd"), lambda.Id.PreviousName);

                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    var file = Path.Combine(directory, string.Format("{0}.zip", useVersion ? (lambda.Version ?? "LATEST") : "LATEST"));

                    if (!File.Exists(file))
                        await webClient.DownloadFileTaskAsync(url, file);
                }
            }
        }

        private async Task DownloadAsync(List<Lambda> lambdas, bool useVersion)
        {
            foreach (var lambda in lambdas)
            {
                var urlRequest = new GetFunctionRequest()
                {
                    FunctionName = lambda.Id.PreviousName
                };

                var urlResponse = await sourceClient.GetFunctionAsync(urlRequest);
                var url = urlResponse.Code.Location;
                using (WebClient webClient = new WebClient())
                {
                    var directory = Path.Combine(directoryPath, "lambdas", Prefix().Trim('_'), DateTime.UtcNow.Date.ToString("yyyyMMdd"), lambda.Id.PreviousName);

                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    await webClient.DownloadFileTaskAsync(url, Path.Combine(directory, string.Format("{0}.zip", useVersion ? (lambda.Version ?? "LATEST") : "LATEST")));
                }
            }
        }

        private List<Lambda> ConvertV2(List<FunctionConfiguration> functions, IamRole role, IEnumerable<AwsSubnet> subnets, IEnumerable<AwsSecurityGroup> groups)
        {
            return functions.Select(item =>
            {
                DateTime? lastModified = null;

                if (DateTime.TryParse(item.LastModified, out var newLastModified))
                    lastModified = newLastModified;

                return new Lambda()
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
                    Role = item.Role,
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
                        SecurityGroupIds = item.VpcConfig?.SecurityGroupIds,
                        SubnetIds = item.VpcConfig?.SubnetIds
                    },
                    LastModifiedOnDestination = lastModified
                };
            }).ToList();
        }

        private List<Lambda> Convert(List<FunctionConfiguration> functions, IamRole role, IEnumerable<AwsSubnet> subnets, IEnumerable<AwsSecurityGroup> groups)
        {
            return functions.Select(item =>
            {
                DateTime? lastModified = null;

                if (DateTime.TryParse(item.LastModified, out var newLastModified))
                    lastModified = newLastModified;

                return new Lambda()
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
                    Role = role != null ? role.Id.NewArn : null,
                    Runtime = item.Runtime,
                    Timeout = item.Timeout,
                    TracingConfig =
                    item.TracingConfig != null ?
                    new TracingConfig()
                    {
                        Mode = item.TracingConfig.Mode
                    } :
                    null,
                    VpcConfig = (subnets != null && groups != null) ? new VpcConfig()
                    {
                        SecurityGroupIds = groups.Select(group => group.Id.NewId).ToList(),
                        SubnetIds = subnets.Select(subnet => subnet.Id.NewId).ToList()
                    } : null,
                    LastModifiedOnDestination = lastModified,
                    Version = item.Version ?? null
                };
            }).ToList();
        }

        public async Task FillStackWithLambdasOnDestinationAsync(IResourceNameFilter previousNamefilter)
        {
            var collection = await ListLambdasOnSourceAsync(previousNamefilter);
            var lambdas = Convert(collection, null, null, null);
            await UpdateWithExistingDataAsync(lambdas);
            stack.Lambdas = lambdas.Where(it => it.Id.NewArn != null).ToList();
        }

        private async Task CreateAsync(List<FunctionConfiguration> data, List<Lambda> lambdas)
        {
            await UpdateWithExistingDataAsync(lambdas);

            var pendingLambdas = lambdas.Where(it =>
                string.IsNullOrWhiteSpace(it.Id.NewArn) ||
                (it.LastModifiedOnSource != null && it.LastModifiedOnDestination != null && it.LastModifiedOnSource.Value > it.LastModifiedOnDestination.Value)).ToList();
            if (pendingLambdas == null || pendingLambdas.Count < 1)
                return;

            await DownloadAsync(pendingLambdas);
            foreach (var lambda in pendingLambdas)
            {
                var filePath = Path.Combine(directoryPath, "lambdas", Prefix().Trim('_'), DateTime.UtcNow.Date.ToString("yyyyMMdd"), string.Format("{0}.zip", lambda.Id.PreviousName.Replace("SummerProd_", string.Empty)));
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
                lambda.LastModifiedOnDestination = DateTime.UtcNow;
            }
        }

        private async Task UpdateWithExistingDataAsync(List<Lambda> lambdas)
        {
            var existingLambdas = await ListLambdasOnDestinationAsync();
            foreach (var lambda in lambdas)
            {
                var existingLambda = existingLambdas.FirstOrDefault(item => item.FunctionName == lambda.Id.NewName);
                if (existingLambda != null)
                {
                    if (DateTime.TryParse(existingLambda.LastModified, out var lastModified))
                        lambda.LastModifiedOnDestination = lastModified;

                    lambda.Id.NewArn = existingLambda.FunctionArn;
                }
            }
        }

        protected string NewNameFor(string name)
        {
            if (name.StartsWith("SummerProd"))
                return name.Replace("SummerProd_", Prefix());

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