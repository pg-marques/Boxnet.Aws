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
            await CreateValidatorsAsync(collection);
            await CreateMethodsAsync(collection);
            await CreateMethodsResponsesAsync(collection);
            await CreateIntegrationAsync(collection);

            stack.Apis = collection;
        }

        private async Task CreateIntegrationAsync(List<AwsApi> collection)
        {
            await AddIntegrationRequestsFromSourceAsync(collection);
        }

        private async Task AddIntegrationRequestsFromSourceAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
            {
                await UpdateIntegrationsWithDestinationDataAsync(api.RootResource, api);
            }
        }

        private async Task UpdateIntegrationsWithDestinationDataAsync(AwsApiResource resource, AwsApi api)
        {
            if (resource.Methods != null)
            {
                foreach (var method in resource.Methods)
                {
                    var request = new GetMethodRequest()
                    {
                        HttpMethod = method.Integration.HttpMethod,
                        ResourceId = method.ResourceId.NewName,
                        RestApiId = method.RestApiId.NewId
                    };

                    var response = await destinationClient.GetMethodAsync(request);

                    if (response?.MethodIntegration != null)
                    {
                        if (method.Integration.Uri == response.MethodIntegration.Uri)
                            method.Integration.IsCreated = true;

                        if (method?.Integration?.Responses != null && response.MethodIntegration.IntegrationResponses != null)
                            foreach (var integrationResponse in method.Integration.Responses)
                            {
                                if (response.MethodIntegration.IntegrationResponses.ContainsKey(integrationResponse.HttpMethod) &&
                                    response.MethodIntegration.IntegrationResponses[integrationResponse.HttpMethod].StatusCode == integrationResponse.StatusCode)
                                    integrationResponse.Created = true;
                            }
                    }

                }

                foreach (var method in resource.Methods)
                {
                    if (method?.Integration != null && !method.Integration.IsCreated)
                    {
                        var request = new PutIntegrationRequest()
                        {
                            CacheKeyParameters = method.Integration.CacheKeyParameters,
                            CacheNamespace = method.ResourceId.NewName,
                            ResourceId = method.ResourceId.NewName,
                            ConnectionId = method.Integration.ConnectionId,
                            ConnectionType = method.Integration.ConnectionType,
                            ContentHandling = method.Integration.ContentHandling,
                            Credentials = method.Integration.Credentials,
                            HttpMethod = method.Integration.HttpMethod,
                            IntegrationHttpMethod = method.Integration.IntegrationHttpMethod,
                            PassthroughBehavior = method.Integration.PassthroughBehavior,
                            RequestParameters = method.Integration.RequestParameters,
                            RequestTemplates = method.Integration.RequestTemplates,
                            RestApiId = method.RestApiId.NewId,
                            TimeoutInMillis = method.Integration.TimeoutInMillis,
                            Type = method.Integration.Type,
                            Uri = method.Integration.Uri
                        };

                        if (request.ResourceId != null)
                        {
                            var response = await destinationClient.PutIntegrationAsync(request);
                            method.Integration.IsCreated = true;
                        }
                    }

                    if (method?.Integration?.Responses != null)
                        foreach (var integrationResponse in method.Integration.Responses)
                        {
                            var integrationResponseRequest = new PutIntegrationResponseRequest()
                            {
                                ContentHandling = integrationResponse.ContentHandling,
                                HttpMethod = integrationResponse.HttpMethod,
                                ResourceId = integrationResponse.ResourceId.NewName,
                                ResponseParameters = integrationResponse.ResponseParameters,
                                ResponseTemplates = integrationResponse.ResponseTemplates,
                                RestApiId = integrationResponse.RestApiId.NewId,
                                SelectionPattern = integrationResponse.SelectionPattern,
                                StatusCode = integrationResponse.StatusCode
                            };

                            var response = await destinationClient.PutIntegrationResponseAsync(integrationResponseRequest);
                            integrationResponse.Created = true;
                        }
                }
            }

            if (resource?.Children != null)
            {
                foreach (var child in resource.Children)
                {
                    await UpdateIntegrationsWithDestinationDataAsync(child, api);
                }
            }
        }

        private async Task CreateMethodsResponsesAsync(List<AwsApi> collection)
        {
            await AddMethodsResponsesFromSourceAsync(collection);
        }

        private async Task AddMethodsResponsesFromSourceAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
            {
                await AddResponsesToResourceAsync(api.RootResource, api);
            }
        }

        private async Task AddResponsesToResourceAsync(AwsApiResource resource, AwsApi api)
        {
            if (resource?.Methods != null)
            {
                foreach (var method in resource.Methods)
                {
                    if (method?.Responses != null)
                    {
                        foreach (var response in method.Responses)
                        {
                            if (response.RequestModels != null)
                                foreach (var model in response.RequestModels)
                                {
                                    var existingModel = api.Models?.FirstOrDefault(it => it.Id.PreviousName == model.Id.PreviousName);
                                    if (existingModel != null)
                                        model.Id = existingModel.Id;
                                }
                        }

                        foreach (var response in method.Responses.Where(it => !it.IsCreated).ToList())
                        {
                            Dictionary<string, string> models = null;
                            if (response.RequestModels != null)
                            {
                                models = new Dictionary<string, string>();
                                foreach (var model in response.RequestModels)
                                {
                                    models.Add(model.ContentType, model.Id.NewName);
                                }
                            }
                            var request = new PutMethodResponseRequest()
                            {
                                HttpMethod = response.HttpMethod,
                                ResourceId = response.ResourceId.NewName,
                                ResponseModels = models,
                                ResponseParameters = response.ResponseParameters,
                                RestApiId = response.RestApiId.NewId,
                                StatusCode = response.StatusCode
                            };
                            var x = 0;

                            var requestResponse = await destinationClient.PutMethodResponseAsync(request);
                            response.IsCreated = true;
                        }
                    };
                }
            }

            if (resource?.Children != null && resource.Children.Count() > 0)
                foreach (var child in resource.Children)
                    await AddResponsesToResourceAsync(child, api);
        }

        private async Task CreateValidatorsAsync(List<AwsApi> collection)
        {
            await AddValidatorsOnSourceAsync(collection);
            await UpdateValidatorsWithDestinationDataAsync(collection);
            await CreateValidatorsOnDestinationAsync(collection);
        }

        private async Task CreateValidatorsOnDestinationAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
            {
                foreach (var validator in api.Validators.Where(it => it.Id.NewId == null).ToList())
                {
                    var request = new CreateRequestValidatorRequest()
                    {
                        Name = validator.Id.NewName,
                        RestApiId = api.Id.NewId,
                        ValidateRequestBody = validator.ValidateRequestBody,
                        ValidateRequestParameters = validator.ValidateRequestParameters
                    };

                    var response = await destinationClient.CreateRequestValidatorAsync(request);

                    validator.Id.NewId = response.Id;

                    await Task.Delay(200);
                }
            }
        }

        private async Task UpdateValidatorsWithDestinationDataAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
            {
                var validators = new List<RequestValidator>();
                string position = null;
                do
                {
                    var request = new GetRequestValidatorsRequest()
                    {
                        Position = position,
                        RestApiId = api.Id.NewId
                    };

                    var response = await destinationClient.GetRequestValidatorsAsync(request);
                    await Task.Delay(200);

                    position = response.Position;

                    validators.AddRange(response.Items);

                } while (position != null);

                foreach (var validator in api.Validators)
                {
                    var existingValidator = validators.FirstOrDefault(it => it.Name == validator.Id.NewName);
                    if (existingValidator != null)
                        validator.Id.NewId = existingValidator.Id;
                }
            }
        }

        private async Task AddValidatorsOnSourceAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
            {
                var validators = new List<RequestValidator>();
                string position = null;
                do
                {
                    var request = new GetRequestValidatorsRequest()
                    {
                        Position = position,
                        RestApiId = api.Id.PreviousId
                    };

                    var response = await sourceClient.GetRequestValidatorsAsync(request);
                    await Task.Delay(200);

                    position = response.Position;

                    validators.AddRange(response.Items);

                } while (position != null);

                foreach (var validator in validators)
                {
                    api.Validators.Add(new AwsApiValidator()
                    {
                        Id = new ResourceIdWithAwsId()
                        {
                            PreviousId = validator.Id,
                            PreviousName = validator.Name,
                            NewName = validator.Name
                        },
                        ApiId = api.Id,
                        ValidateRequestBody = validator.ValidateRequestBody,
                        ValidateRequestParameters = validator.ValidateRequestParameters
                    });
                }
            }
        }

        private async Task CreateAuthorizersAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
            {
                var authorizersCollection = await GetAuthorizersOnSourceAsync(api);

                foreach (var authorizerData in authorizersCollection)
                {
                    string authorizerUri = null;
                    List<string> providersARNS = null;

                    if (authorizerData.Type == AuthorizerType.COGNITO_USER_POOLS)
                    {
                        providersARNS = authorizerData.ProviderARNs.Select(it => stack?.UsersPools?.FirstOrDefault(u => u.Id.PreviousArn == it).Id.NewArn).ToList();
                    }
                    else if (authorizerData.Type == AuthorizerType.REQUEST)
                    {
                        var lambda = stack?.Lambdas?.FirstOrDefault(it => authorizerData?.AuthorizerUri != null && authorizerData.AuthorizerUri.Contains(it.Id.PreviousArn));

                        if (lambda != null)
                            authorizerUri = string.Format("arn:aws:apigateway:us-east-1:lambda:path/2015-03-31/functions/{0}/invocations", lambda.Id.NewArn);
                    }

                    api.Authorizers.Add(new AwsApiAuthorizer()
                    {
                        AuthorizerCredentials = authorizerData.AuthorizerCredentials,
                        AuthorizerResultTtlInSeconds = authorizerData.AuthorizerResultTtlInSeconds,
                        AuthorizerUri = authorizerUri,
                        AuthType = authorizerData.AuthType,
                        Id = new AwsAuthorizerId()
                        {
                            PreviousId = authorizerData.Id,
                            PreviousName = authorizerData.Name,
                            NewName = NewNameFor(authorizerData.Name)
                        },
                        IdentitySource = authorizerData.IdentitySource,
                        IdentityValidationExpression = authorizerData.IdentityValidationExpression,
                        ProviderARNs = providersARNS,
                        RestApiId = api.Id,
                        Type = authorizerData.Type
                    });
                }

                var existingAuthorizers = await GetAuthorizersOnDestinationAsync(api);

                foreach (var authorizer in api.Authorizers)
                {
                    var existingAuthorizer = existingAuthorizers.FirstOrDefault(it => it.Name == authorizer.Id.NewName);

                    if (existingAuthorizer != null)
                        authorizer.Id.NewId = existingAuthorizer.Id;
                }

                foreach (var authorizer in api.Authorizers.Where(it => it.Id.NewId == null).ToList())
                {
                    var authRequest = new CreateAuthorizerRequest()
                    {
                        AuthorizerCredentials = authorizer.AuthorizerCredentials,
                        AuthorizerResultTtlInSeconds = authorizer.AuthorizerResultTtlInSeconds,
                        AuthorizerUri = authorizer.AuthorizerUri,
                        Type = authorizer.Type,
                        AuthType = authorizer.AuthType,
                        IdentitySource = authorizer.IdentitySource,
                        IdentityValidationExpression = authorizer.IdentityValidationExpression,
                        Name = authorizer.Id.NewName,
                        ProviderARNs = authorizer.ProviderARNs,
                        RestApiId = api.Id.NewId
                    };

                    var authResponse = await destinationClient.CreateAuthorizerAsync(authRequest);

                    authorizer.Id.NewId = authResponse.Id;
                    await Task.Delay(200);
                }
            }
        }

        private async Task<List<Authorizer>> GetAuthorizersOnDestinationAsync(AwsApi api)
        {
            var existingAuthorizers = new List<Authorizer>();
            string position = null;
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
                await Task.Delay(200);

            } while (position != null);
            return existingAuthorizers;
        }

        private async Task<List<Authorizer>> GetAuthorizersOnSourceAsync(AwsApi api)
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
                await Task.Delay(200);

            } while (position != null);
            return authorizers;
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
            foreach (var model in api.Models.Where(it => it.Id.NewId == null).ToList())
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
                await Task.Delay(200);
            }
        }

        private void UpdateModels(AwsApi api, List<Model> existingCollection)
        {
            foreach (var model in api.Models)
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
                await Task.Delay(200);
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
                await Task.Delay(200);

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
            if (method == null) return;

            var request = new GetResourceRequest()
            {
                ResourceId = resource.Id.NewName,
                RestApiId = resource.RestApiId.NewId
            };

            var response = await destinationClient.GetResourceAsync(request);
            await Task.Delay(200);

            if (response?.ResourceMethods != null && !response.ResourceMethods.ContainsKey(method.Verb))
            {
                var models = new Dictionary<string, string>();
                foreach (var model in method.RequestModels)
                    models.Add(model.ContentType, model.Id.NewName);

                var methodRequest = new PutMethodRequest()
                {
                    ApiKeyRequired = method.ApiKeyRequired,
                    AuthorizationScopes = method.AuthorizationScopes,
                    AuthorizationType = method.AuthorizationType ?? "NONE",
                    AuthorizerId = api.Authorizers?.FirstOrDefault(it => it.Id.PreviousId == method.AuthorizerId)?.Id.NewId,
                    HttpMethod = method.Verb,
                    OperationName = method.OperationName,
                    RequestModels = models,
                    RequestParameters = method.RequestParameters,
                    RequestValidatorId = api?.Validators?.FirstOrDefault(it => it.Id.PreviousId == method.RequestValidatorId)?.Id?.NewId,
                    ResourceId = resource.Id.NewName,
                    RestApiId = resource.RestApiId.NewId
                };

                var methodResponse = await destinationClient.PutMethodAsync(methodRequest);
                await Task.Delay(200);
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

            await Task.Delay(200);
        }

        private async Task CreateResourcesAsync(List<AwsApi> collection)
        {
            foreach (var api in collection)
            {
                var resourcesCollection = await GetResourcesFromSourceAsync(api);
                var rootResource = resourcesCollection.FirstOrDefault(it => it.Path == "/" && it.PathPart == null);
                api.RootResource = await ConvertAsync(rootResource, null, api, resourcesCollection);
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
                await Task.Delay(200);
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
                if (resource.Methods != null)
                    foreach (var method in resource.Methods)
                    {
                        if (item.ResourceMethods != null && item.ResourceMethods.ContainsKey(method?.Verb))
                        {
                            var existingMethod = item.ResourceMethods.FirstOrDefault(it => it.Key == method?.Verb).Value;

                            if (method?.Responses != null)
                                foreach (var response in method.Responses)
                                {
                                    if (existingMethod?.MethodResponses != null && existingMethod.MethodResponses.ContainsKey(response.StatusCode))
                                        response.IsCreated = true;
                                }

                        }
                    }
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
                await Task.Delay(200);
                resources.AddRange(response.Items);

                position = response.Position;
            } while (position != null);

            foreach (var resource in resources)
            {
                if (resource.ResourceMethods != null)
                    foreach (var method in resource.ResourceMethods)
                    {
                        var request = new GetMethodRequest()
                        {
                            HttpMethod = method.Key,
                            ResourceId = resource.Id,
                            RestApiId = item.Id.NewId
                        };

                        var response = await destinationClient.GetMethodAsync(request);

                        if (response?.MethodResponses != null)
                            method.Value.MethodResponses = response.MethodResponses;

                        if (response?.MethodIntegration != null)
                            method.Value.MethodIntegration = response.MethodIntegration;

                        await Task.Delay(200);
                    }
            }

            return resources;
        }

        private async Task<AwsApiResource> ConvertAsync(Resource item, AwsApiResource parent, AwsApi api, List<Resource> resourcesCollection)
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

            if (item.ResourceMethods != null && item.ResourceMethods.Count > 0)
            {
                foreach (var method in item.ResourceMethods)
                {
                    var request = new GetMethodRequest()
                    {
                        HttpMethod = method.Key,
                        ResourceId = item.Id,
                        RestApiId = api.Id.PreviousId
                    };

                    var response = await sourceClient.GetMethodAsync(request);
                    if (response?.MethodResponses != null && response.MethodResponses.Count() > 0 && item.ResourceMethods.ContainsKey(method.Key))
                        item.ResourceMethods[method.Key].MethodResponses = response.MethodResponses;

                    if (response?.MethodIntegration != null && item.ResourceMethods.ContainsKey(method.Key))
                    {
                        item.ResourceMethods[method.Key].MethodIntegration = response.MethodIntegration;
                    }
                }

                foreach (var method in item.ResourceMethods)
                {
                    string uri = null;
                    if (!string.IsNullOrWhiteSpace(method.Value?.MethodIntegration?.Uri))
                    {
                        var lambda = stack.Lambdas.FirstOrDefault(it => method.Value.MethodIntegration.Uri.Contains(it.Id.PreviousArn));

                        if (lambda != null)
                            uri = method.Value.MethodIntegration.Uri.Replace(lambda.Id.PreviousArn, lambda.Id.NewArn);
                    }

                    resource.Methods.Add(new AwsApiMethod()
                    {
                        Verb = method.Key,
                        RestApiId = api.Id,
                        ResourceId = resource.Id,
                        Responses = method.Value?.MethodResponses?.Select(it => new AwsApiMethodResponse()
                        {
                            HttpMethod = method.Key,
                            ResourceId = resource.Id,
                            ResponseParameters = it.Value?.ResponseParameters,
                            RequestModels = it.Value?.ResponseModels?.Select(model => new AwsApiModel()
                            {
                                ContentType = model.Key,
                                Id = new ResourceIdWithAwsId()
                                {
                                    PreviousName = model.Value
                                }
                            }).ToList(),
                            RestApiId = api.Id,
                            StatusCode = it.Value?.StatusCode
                        })?.ToList(),
                        Integration = new AwsApiMethodIntegration()
                        {
                            ResourceId = resource.Id,
                            RestApiId = api.Id,
                            CacheKeyParameters = method.Value.MethodIntegration.CacheKeyParameters,
                            ConnectionId = method.Value.MethodIntegration.ConnectionId,
                            ConnectionType = method.Value.MethodIntegration.ConnectionType,
                            ContentHandling = method.Value.MethodIntegration.ContentHandling,
                            Credentials = method.Value.MethodIntegration.Credentials,
                            HttpMethod = method.Key,
                            IntegrationHttpMethod = method.Value.MethodIntegration.HttpMethod,
                            PassthroughBehavior = method.Value.MethodIntegration.PassthroughBehavior,
                            RequestParameters = method.Value.MethodIntegration.RequestParameters,
                            RequestTemplates = method.Value.MethodIntegration.RequestTemplates,
                            TimeoutInMillis = method.Value.MethodIntegration.TimeoutInMillis,
                            Type = method.Value.MethodIntegration.Type,
                            Uri = uri,
                            Responses = method.Value.MethodIntegration.IntegrationResponses?.Select(it => new AwsApiMethodIntegrationResponse()
                            {
                                RestApiId = api.Id,
                                ContentHandling = it.Value.ContentHandling,
                                HttpMethod = method.Key,
                                ResponseParameters = it.Value.ResponseParameters,
                                ResponseTemplates = it.Value.ResponseTemplates,
                                SelectionPattern = it.Value.SelectionPattern,
                                StatusCode = it.Value.StatusCode,
                                ResourceId = resource.Id
                            }).ToList()
                        }
                    });
                }
            }

            var childrenItems = resourcesCollection.Where(it => it.ParentId == resource.Id.PreviousName).ToList();

            if (childrenItems != null && childrenItems.Count() > 0)
                foreach (var child in childrenItems)
                    resource.Children.Add(await ConvertAsync(child, resource, api, resourcesCollection));

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
                await Task.Delay(200);
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
                await Task.Delay(200);
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
                await Task.Delay(200);

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
                await Task.Delay(200);
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
