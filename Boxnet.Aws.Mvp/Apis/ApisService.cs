using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.APIGateway;
using Amazon.APIGateway.Model;
using Amazon.Runtime;

namespace Boxnet.Aws.Mvp.Apis
{
    public class ApisService : IDisposable
    {
        protected readonly AmazonAPIGatewayClient sourceClient;
        protected readonly AmazonAPIGatewayClient destinationClient;
        protected readonly Stack stack;
        protected readonly Dictionary<string, string> tags;

        public ApisService(
            Stack stack,
            string sourceAccessKey,
            string sourceSecretKey,
            string sourceRegion,
            string destinationAccessKey,
            string destinationSecretKey,
            string destinationRegion)
        {
            this.stack = stack;
            sourceClient = new AmazonAPIGatewayClient(new BasicAWSCredentials(sourceAccessKey, sourceSecretKey), RegionEndpoint.GetBySystemName(sourceRegion));
            destinationClient = new AmazonAPIGatewayClient(new BasicAWSCredentials(destinationAccessKey, destinationSecretKey), RegionEndpoint.GetBySystemName(destinationRegion));
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

        public async Task CopyAsync(IResourceNameFilter filter)
        {
            var collectionData = await GetSourceApisAsync(filter);
            var collection = Convert(collectionData);
            await CreateAsync(collection);
            await CreateResourcesAsync(collection);
        }

        private async Task CreateResourcesAsync(List<AwsApi> collection)
        {
            foreach (var item in collection)
            {
                var resourcesCollection = await GetResourcesFromSourceAsync(item);
                item.Resources = Convert(resourcesCollection, item);
            }

            foreach (var item in collection)
            {
                var existingCollection = await GetResourcesFromDestinationAsync(item);
                var root = item.RootResource;
                var newRoot = existingCollection.FirstOrDefault(it => it.ParentId == null && it.Path == "/" && it.PathPart == null);
                root.Id.NewName = newRoot.Id;
                foreach (var resource in item.Resources)
                {
                    var existingItem = existingCollection.FirstOrDefault(existing => existing.PathPart == resource.PathPart);

                    if (existingItem != null)
                        resource.Id.NewName = existingItem.Id;
                }

            }

            
            foreach (var item in collection)
            {
                var root = item.RootResource;

                //if (root != null)
                //{
                //    var rootRequest = new GetResourcesRequest()
                //    {
                        
                //        PathPart = root.PathPart,
                //        RestApiId = root.RestApiId.NewId
                //    };

                //    var rootResponse = await destinationClient.CreateResourceAsync(rootRequest);
                //    root.Id.NewName = rootResponse.Id;
                //}

                var filtered = item.Resources.Where(it => string.IsNullOrWhiteSpace(it.Id.NewName)).ToList();

                foreach (var child in filtered.Where(it => it.ParentId?.PreviousName == root.Id.PreviousName))
                {
                    child.ParentId.NewName = root.Id.NewName;
                }

                while(filtered.Any(it => it != root && string.IsNullOrWhiteSpace(it.ParentId?.NewName)))
                {
                    var children = filtered.OrderBy(it => it.PathPart?.Length).ToList();

                    foreach (var child in children)
                    {
                        if (child.Levels?.Count() > 0)
                        {
                            var parent = item.Resources.FirstOrDefault(it =>
                            {
                                if (it == child)
                                    return false;

                                if (it.Levels == null || child.Levels == null) return false;

                                var hasCorrectSize = it.Levels?.Count() == child.Levels?.Count() - 1;

                                for (var i = 0; i < it.Levels.Count(); i++)
                                {
                                    if (it.Levels.ElementAt(i) != child.Levels.ElementAt(i))
                                        return false;
                                }

                                return true;
                            });

                            if (parent != null && (child.ParentId.PreviousName != parent.Id.PreviousName))
                                child.ParentId = parent.Id;
                        }
                    }

                    foreach(var child in children.Where(it => it != root && it.ParentId.NewName != null).ToList())
                    {
                        var request = new CreateResourceRequest()
                        {                            
                            ParentId = child.ParentId.NewName,
                            PathPart = child.PathPart,
                            RestApiId = item.Id.NewId
                        };

                        var response = await destinationClient.CreateResourceAsync(request);
                        child.Id.NewName = response.Id;

                        foreach(var grandChild in filtered.Where(it => it.ParentId.PreviousName == child.Id.PreviousName).ToList())
                        {
                            grandChild.ParentId.NewName = child.Id.NewName;
                        }
                    }

                    
                }
                


            }
        }

        private async Task<List<Resource>> GetResourcesFromDestinationAsync(AwsApi item)
        {
            var resources = new List<Resource>();
            string position = null;
            do
            {
                var request = new GetResourcesRequest()
                {
                    Position = position,
                    RestApiId = item.Id.NewId
                };

                var response = await destinationClient.GetResourcesAsync(request);
                await Task.Delay(1000);
                resources.AddRange(response.Items);

                position = response.Position;
            } while (position != null);

            return resources;
        }

        private List<AwsApiResource> Convert(List<Resource> resourcesCollection, AwsApi api)
        {
            return resourcesCollection.Select(item => new AwsApiResource()
            {
                Id = new ResourceId()
                {
                    PreviousName = item.Id
                },
                ParentId = new ResourceId()
                {
                    PreviousName = item.ParentId
                },
                PathPart = item.PathPart,
                RestApiId = api.Id
            }).ToList();
        }

        private async Task<List<Resource>> GetResourcesFromSourceAsync(AwsApi item)
        {
            var resources = new List<Resource>();
            string position = null;
            do
            {
                var request = new GetResourcesRequest()
                {
                    Position = position,
                    RestApiId = item.Id.PreviousId
                };

                var response = await sourceClient.GetResourcesAsync(request);
                await Task.Delay(1000);
                resources.AddRange(response.Items);

                position = response.Position;
            } while (position != null);

            return resources;
        }

        private async Task CreateAsync(List<AwsApi> collection)
        {
            var existingCollection = await GetDestinationApisAsync();
            foreach (var existingApi in existingCollection)
            {
                var api = collection.FirstOrDefault(item => item.Id.NewName == existingApi.Name);

                if (api != null)
                {
                    api.Id.NewId = existingApi.Id;
                }
            }

            var pendingCollection = collection.Where(item => string.IsNullOrEmpty(item.Id.NewId)).ToList();

            foreach (var item in pendingCollection)
            {
                var request = new CreateRestApiRequest()
                {
                    ApiKeySource = item.ApiKeySource,
                    BinaryMediaTypes = item.BinaryMediaTypes,
                    Description = item.Description,
                    EndpointConfiguration = item.EndpointConfiguration,
                    MinimumCompressionSize = item.MinimumCompressionSize,
                    Name = item.Id.NewName,
                    Policy = item.Policy,
                    Version = item.Version
                };

                var response = await destinationClient.CreateRestApiAsync(request);
                item.Id.NewId = response.Id;
                await Task.Delay(1000);
            }
        }

        private List<AwsApi> Convert(List<RestApi> collectionData)
        {
            return collectionData.Select(item => new AwsApi()
            {
                Id = new ResourceIdWithAwsId()
                {
                    PreviousId = item.Id,
                    PreviousName = item.Name,
                    NewName = NewNameFor(item.Name)
                },
                BinaryMediaTypes = item.BinaryMediaTypes,
                ApiKeySource = item.ApiKeySource,
                Description = item.Description,
                EndpointConfiguration = item.EndpointConfiguration,
                MinimumCompressionSize = item.MinimumCompressionSize,
                Policy = item.Policy,
                Version = item.Version
            }).ToList();
        }

        protected string NewNameFor(string name)
        {
            if (name.StartsWith("SummerProd"))
                return name.Replace("SummerProd.", Prefix());

            return string.Format("{0}{1}", Prefix(), name);
        }

        protected string Prefix()
        {
            return string.Format("{0}{1}.", stack.Name, stack.Environment);
        }

        private async Task<List<RestApi>> GetDestinationApisAsync()
        {
            var apis = new List<RestApi>();
            var filter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            string position = null;
            do
            {
                var request = new GetRestApisRequest()
                {
                    Position = position
                };

                var response = await destinationClient.GetRestApisAsync(request);
                await Task.Delay(1000);

                apis.AddRange(response.Items.Where(item => filter.IsValid(item.Name)));

                position = response.Position;

            } while (position != null);

            return apis;
        }

        private async Task<List<RestApi>> GetSourceApisAsync(IResourceNameFilter filter)
        {
            var apis = new List<RestApi>();
            var newApisFilter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            string position = null;
            do
            {
                var request = new GetRestApisRequest()
                {
                    Position = position
                };

                var response = await sourceClient.GetRestApisAsync(request);

                apis.AddRange(response.Items.Where(item => !newApisFilter.IsValid(item.Name) && filter.IsValid(item.Name)));

                position = response.Position;

            } while (position != null);

            return apis;
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
