using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
            await CreateModelsAsync(collection);
            await CreateAuthorizersAsync(collection);
            await CreateMethodsAsync(collection);
        }

        private async Task CreateAuthorizersAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
            {
                var authorizers = new List<Authorizer>();
                string position = null;
                do
                {
                    var request = new GetAuthorizersRequest()
                    {
                        Position = position,
                        RestApiId = api.Id.PreviousId
                    };

                    var response = await sourceClient.GetAuthorizersAsync(request);

                    authorizers.AddRange(response.Items);

                    position = response.Position;

                } while (position != null);

                var existingAuthorizers = new List<Authorizer>();
                position = null;
                do
                {
                    var request = new GetAuthorizersRequest()
                    {
                        Position = position,
                        RestApiId = api.Id.NewId
                    };

                    var response = await destinationClient.GetAuthorizersAsync(request);

                    existingAuthorizers.AddRange(response.Items);

                    position = response.Position;

                } while (position != null);

                foreach (var authorizerData in authorizers)
                {
                    var newName = NewNameFor(authorizerData.Name);
                    var existingAuthorizer = existingAuthorizers.FirstOrDefault(it => it.Name == newName);

                    if (existingAuthorizer != null)
                        authorizer
                }

                foreach (var authorizerData in authorizers)
                {
                    if (authorizerData.Type == AuthorizerType.COGNITO_USER_POOLS)
                    {
                        var authRequest = new CreateAuthorizerRequest()
                        {
                            AuthorizerCredentials = authorizerData.AuthorizerCredentials,
                            AuthorizerResultTtlInSeconds = authorizerData.AuthorizerResultTtlInSeconds,
                            AuthorizerUri = authorizerData.AuthorizerUri,
                            Type = authorizerData.Type,
                            AuthType = authorizerData.AuthType,
                            IdentitySource = authorizerData.IdentitySource,
                            IdentityValidationExpression = authorizerData.IdentityValidationExpression,
                            Name = NewNameFor(authorizerData.Name),
                            ProviderARNs = authorizerData.ProviderARNs.Select(it => stack?.UsersPools?.FirstOrDefault(u => u.Id.PreviousArn == it).Id.NewArn).ToList(),
                            RestApiId = api.Id.NewId
                        };
                    } else if (authorizerData.Type == AuthorizerType.REQUEST)
                    {
                        string uri = null;
                        var lambda = stack?.Lambdas?.FirstOrDefault(it => authorizerData?.AuthorizerUri != null && authorizerData.AuthorizerUri.Contains(it.Id.PreviousArn));

                        if (lambda != null)
                            uri = string.Format("arn:aws:apigateway:us-east-1:lambda:path/2015-03-31/functions/{0}/invocations", lambda.Id.NewArn);

                        var authRequest = new CreateAuthorizerRequest()
                        {
                            AuthorizerCredentials = authorizerData.AuthorizerCredentials,
                            AuthorizerResultTtlInSeconds = authorizerData.AuthorizerResultTtlInSeconds,
                            AuthorizerUri = uri,
                            Type = authorizerData.Type,
                            AuthType = authorizerData.AuthType,
                            IdentitySource = authorizerData.IdentitySource,
                            IdentityValidationExpression = authorizerData.IdentityValidationExpression,
                            Name = NewNameFor(authorizerData.Name),
                            ProviderARNs = authorizerData.ProviderARNs,
                            RestApiId = api.Id.NewId
                        };
                    }
                }

            }


        }

        private async Task CreateModelsAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
            {
                var modelsCollection = await GetModelsFromSourceAsync(api);
                api.Models = Convert(modelsCollection, api);
                var existingCollection = await GetModelsFromDestinationAsync(api);
                UpdateModels(api, existingCollection);
                await CreateNonExistingModelAsync(api);
            }
        }

        private async Task CreateNonExistingModelAsync(AwsApi api)
        {
            foreach(var model in api.Models.Where(it => it.Id.NewId == null).ToList())
            {
                var request = new CreateModelRequest()
                {
                    RestApiId = api.Id.NewId,
                    ContentType = model.ContentType,
                    Description = model.Description,
                    Name = model.Id.NewName,
                    Schema = model.Schema
                };

                var response = await destinationClient.CreateModelAsync(request);
                model.Id.NewId = response.Id;
            }
        }

        private void UpdateModels(AwsApi api, List<Model> existingCollection)
        {
            foreach(var model in api.Models)
            {
                var existingModel = existingCollection.FirstOrDefault(it => it.Name == model.Id.PreviousName);
                if (existingModel != null)
                    model.Id.NewId = existingModel.Id;
            }
        }

        private List<AwsApiModel> Convert(List<Model> collection, AwsApi api)
        {
            return collection.Select(item => new AwsApiModel()
            {
                Id = new ResourceIdWithAwsId()
                {
                    PreviousId = item.Id,
                    PreviousName = item.Name,
                    NewName = item.Name,
                },
                ContentType = item.ContentType,
                Description = item.Description,
                Schema = HttpUtility.UrlDecode(item.Schema),
                RestApiId = api.Id
            }).ToList();
        }

        private async Task<List<Model>> GetModelsFromDestinationAsync(AwsApi api)
        {
            var models = new List<Model>();

            string position = null;
            do
            {
                var request = new GetModelsRequest()
                {
                    RestApiId = api.Id.NewId,
                    Position = position
                };

                var response = await destinationClient.GetModelsAsync(request);

                models.AddRange(response.Items);

                position = response.Position;

            } while (position != null);

            return models;
        }

        private async Task<List<Model>> GetModelsFromSourceAsync(AwsApi api)
        {
            var models = new List<Model>();

            string position = null;
            do
            {
                var request = new GetModelsRequest()
                {
                    RestApiId = api.Id.PreviousId,
                    Position = position
                };

                var response = await sourceClient.GetModelsAsync(request);

                models.AddRange(response.Items);

                position = response.Position;

            } while (position != null);

            return models;
        }

        private async Task CreateMethodsAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
                await CreateMethodsForResourceAsync(api.RootResource, api);

        }

        private async Task CreateMethodsForResourceAsync(AwsApiResource resource, AwsApi api)
        {
            if (resource.Methods != null && resource.Methods.Count > 0)
                foreach (var method in resource.Methods)
                {
                    await UpdateMethodsWithSourceAsync(resource, api, method);
                    await UpdateMethodsWithDestinationAsync(resource, api, method);
                }

            if (resource.Children != null && resource.Children.Count() > 0)
                foreach (var child in resource.Children)
                    await CreateMethodsForResourceAsync(child, api);
        }

        private async Task UpdateMethodsWithDestinationAsync(AwsApiResource resource, AwsApi api, AwsApiMethod method)
        {            
            var request = new GetResourceRequest()
            {
                ResourceId = resource.Id.NewName,
                RestApiId = resource.RestApiId.NewId
            };

            var response = await destinationClient.GetResourceAsync(request);

            if (response?.ResourceMethods != null && !response.ResourceMethods.ContainsKey(method.Verb))
            {
                var models = new Dictionary<string, string>();
                foreach (var model in method.RequestModels)
                    models.Add(model.ContentType, model.Id.NewName);

                var methodRequest = new PutMethodRequest()
                {
                    ApiKeyRequired = method.ApiKeyRequired,
                    AuthorizationScopes = method.AuthorizationScopes,
                    AuthorizationType = method.AuthorizationType,
                    //AuthorizerId = method.AuthorizerId,
                    HttpMethod = method.Verb,
                    OperationName = method.OperationName,
                    RequestModels = models,
                    RequestParameters = method.RequestParameters,
                    RequestValidatorId = method.RequestValidatorId,
                    ResourceId = resource.Id.NewName,
                    RestApiId = resource.RestApiId.NewId                    
                };

                

                var methodResponse = await destinationClient.PutMethodAsync(methodRequest);
            }
            
            
        }

        private async Task UpdateMethodsWithSourceAsync(AwsApiResource resource, AwsApi api, AwsApiMethod method)
        {
            var request = new GetMethodRequest()
            {
                HttpMethod = method.Verb,
                ResourceId = resource.Id.PreviousName,
                RestApiId = resource.RestApiId.PreviousId
            };

            var response = await sourceClient.GetMethodAsync(request);
            method.RequestModels = api.Models.Where(it => response.RequestModels.Any(i => i.Value == it.Id.PreviousName && i.Key == it.ContentType)).ToList();
            method.RequestValidatorId = response.RequestValidatorId;
            method.OperationName = response.OperationName;
            method.RequestParameters = response.RequestParameters;
        }

        private async Task CreateResourcesAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
            {
                var resourcesCollection = await GetResourcesFromSourceAsync(api);
                var rootResource = resourcesCollection.FirstOrDefault(it => it.Path == "/" && it.PathPart == null);
                api.RootResource = Convert(rootResource, null, api, resourcesCollection);
                var existingResourcesCollection = await GetResourcesFromDestinationAsync(api);
                UpdateWithExistingResourceData(api.RootResource, existingResourcesCollection);

                await CreateNonExistingResourceAsync(api.RootResource);
            }
        }

        private async Task CreateNonExistingResourceAsync(AwsApiResource resource)
        {
            //local/countryId
            if (resource.Id.NewName == null)
            {
                var request = new CreateResourceRequest()
                {
                    ParentId = resource.ParentId.NewName,
                    PathPart = resource.PathPart,
                    RestApiId = resource.RestApiId.NewId
                };

                var response = await destinationClient.CreateResourceAsync(request);
                resource.Id.NewName = response.Id;
                await Task.Delay(1000);
            }

            if (resource.Children != null && resource.Children.Count() > 0)
                foreach (var child in resource.Children)
                    await CreateNonExistingResourceAsync(child);
        }

        private void UpdateWithExistingResourceData(AwsApiResource resource, IEnumerable<Resource> existingResources)
        {
            var item = existingResources.FirstOrDefault(it => it.Path == resource.Path && it.PathPart == resource.PathPart);
            if (item != null)
            {
                resource.Id.NewName = item.Id;
            }
            if (resource.Children != null && resource.Children.Count() > 0)
                foreach (var child in resource.Children)
                    UpdateWithExistingResourceData(child, existingResources);

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

        private AwsApiResource Convert(Resource item, AwsApiResource parent, AwsApi api, List<Resource> resourcesCollection)
        {
            var resource = new AwsApiResource()
            {
                Id = new ResourceId()
                {
                    PreviousName = item.Id
                },
                ParentId = parent?.Id,
                Path = item.Path,
                PathPart = item.PathPart,
                RestApiId = api.Id
            };

            var childrenItems = resourcesCollection.Where(it => it.ParentId == resource.Id.PreviousName).ToList();

            if (childrenItems != null && childrenItems.Count() > 0)
                foreach (var child in childrenItems)
                    resource.Children.Add(Convert(child, resource, api, resourcesCollection));

            if (item.ResourceMethods != null && item.ResourceMethods.Count > 0)
                foreach (var method in item.ResourceMethods)
                    resource.Methods.Add(new AwsApiMethod()
                    {
                        Verb = method.Key,
                        RestApiId = api.Id,
                        ResourceId = resource.Id
                    });

            return resource;
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
