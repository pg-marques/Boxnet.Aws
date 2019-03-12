using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;

namespace Boxnet.Aws.Mvp.Cognito
{
    public class UsersPoolsService : IDisposable
    {
        protected readonly AmazonCognitoIdentityProviderClient sourceClient;
        protected readonly AmazonCognitoIdentityProviderClient destinationClient;
        protected readonly Stack stack;
        protected readonly Dictionary<string, string> tags;


        public UsersPoolsService(
            Stack stack,
            string sourceAccessKey,
            string sourceSecretKey,
            string sourceRegion,
            string destinationAccessKey,
            string destinationSecretKey,
            string destinationRegion)
        {
            this.stack = stack;
            sourceClient = new AmazonCognitoIdentityProviderClient(new BasicAWSCredentials(sourceAccessKey, sourceSecretKey), RegionEndpoint.GetBySystemName(sourceRegion));
            destinationClient = new AmazonCognitoIdentityProviderClient(new BasicAWSCredentials(destinationAccessKey, destinationSecretKey), RegionEndpoint.GetBySystemName(destinationRegion));
            tags = new Dictionary<string, string>()
            {
                { "Project", stack.Name },
                { "Environment", stack.Environment },
                { "ProjectEnvironment", string.Format("{0}{1}", stack.Name, stack.Environment) }
            };
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

        public async Task CopyAsync(IResourceNameFilter filter)
        {
            var collectionData = await ListUserPoolsFromSourceAsync(filter);
            var collectionDataWithDetails = await GetUserPoolsFromSourceAsync(collectionData);
            var collection = Convert(collectionDataWithDetails);
            await CreateUserPoolAsync(collection);
            stack.UsersPools = collection;
        }

        public async Task FillStackWithUserPoolsOnDestinationAsync(IResourceNameFilter previousNamefilter)
        {
            var collectionData = await ListUserPoolsFromSourceAsync(previousNamefilter);
            var collectionDataWithDetails = await GetUserPoolsFromSourceAsync(collectionData);
            var collection = Convert(collectionDataWithDetails);
            await UpdateWithExistingDataAsync(collection);
            
            stack.UsersPools = collection.Where(it => it.Id.NewArn != null).ToList();
        }

        private async Task CreateUserPoolAsync(List<UserPool> collection)
        {
            await UpdateWithExistingDataAsync(collection);

            foreach (var userPool in collection.Where(it => it.Id.NewId == null).ToList())
            {
                var request = new CreateUserPoolRequest()
                {
                    AdminCreateUserConfig = userPool.AdminCreateUserConfig,
                    AliasAttributes = userPool.AliasAttributes,
                    AutoVerifiedAttributes = userPool.AutoVerifiedAttributes,
                    DeviceConfiguration = userPool.DeviceConfiguration,
                    EmailConfiguration = userPool.EmailConfiguration,
                    EmailVerificationMessage = userPool.EmailVerificationMessage,
                    EmailVerificationSubject = userPool.EmailVerificationSubject,
                    LambdaConfig = userPool.LambdaConfig,
                    MfaConfiguration = userPool.MfaConfiguration,
                    Policies = userPool.Policies,
                    PoolName = userPool.Id.NewName,
                    Schema = userPool.Schema.Where(it => !it.Name.StartsWith("custom:")).ToList(),
                    SmsAuthenticationMessage = userPool.SmsAuthenticationMessage,
                    SmsConfiguration = userPool.SmsConfiguration,
                    SmsVerificationMessage = userPool.SmsVerificationMessage,
                    UsernameAttributes = userPool.UsernameAttributes,
                    UserPoolAddOns = userPool.UserPoolAddOns,
                    UserPoolTags = tags,
                    VerificationMessageTemplate = userPool.VerificationMessageTemplate
                };


                var response = await destinationClient.CreateUserPoolAsync(request);
                userPool.Id.NewId = response.UserPool.Id;
                userPool.Id.NewArn = response.UserPool.Arn;
                var customAttributes = userPool.Schema
                        .Where(it => it.Name.StartsWith("custom:"))
                        .Select(it => new SchemaAttributeType()
                        {
                            AttributeDataType = it.AttributeDataType,
                            DeveloperOnlyAttribute = it.DeveloperOnlyAttribute,
                            Mutable = it.Mutable,
                            Name = it.Name.Replace("custom:", string.Empty),
                            NumberAttributeConstraints = it.NumberAttributeConstraints,
                            Required = it.Required,
                            StringAttributeConstraints = it.StringAttributeConstraints
                        })
                        .ToList();

                if (customAttributes != null && customAttributes.Count() > 0)
                {
                    var attributeRequest = new AddCustomAttributesRequest()
                    {
                        UserPoolId = userPool.Id.NewId,
                        CustomAttributes = customAttributes
                    };

                    var attributeResponse = await destinationClient.AddCustomAttributesAsync(attributeRequest);
                }
            }

        }

        private async Task UpdateWithExistingDataAsync(List<UserPool> collection)
        {
            var existingUserPools = await ListUserPoolsFromDestinationAsync();
            foreach (var item in collection)
            {
                var existingUserPool = existingUserPools.FirstOrDefault(it => it.Name == item.Id.NewName);
                if (existingUserPool != null)
                {
                    item.Id.NewId = existingUserPool.Id;
                    item.Id.NewArn = existingUserPool.Arn;
                }
            }
        }

        private async Task<List<UserPoolType>> ListUserPoolsFromDestinationAsync()
        {
            var pools = new List<UserPoolType>();

            var dataCollection = new List<UserPoolDescriptionType>();

            var filter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            string token = null;
            do
            {
                var request = new ListUserPoolsRequest()
                {
                    NextToken = token,
                    MaxResults = 60
                };

                var response = await destinationClient.ListUserPoolsAsync(request);

                dataCollection.AddRange(response.UserPools.Where(it => filter.IsValid(it.Name)));

                token = response.NextToken;
            } while (token != null);

            foreach (var item in dataCollection)
            {
                var request = new DescribeUserPoolRequest()
                {
                    UserPoolId = item.Id
                };

                var response = await destinationClient.DescribeUserPoolAsync(request);
                pools.Add(response.UserPool);
            }

            return pools;
        }

        private List<UserPool> Convert(List<UserPoolType> collectionData)
        {
            return collectionData.Select(item =>
            {
                var lambdaConfig = item.LambdaConfig;
                var smsConfiguration = item.SmsConfiguration;
                var schema = item?.SchemaAttributes?.Where(it =>
                    (it.NumberAttributeConstraints != null && it.NumberAttributeConstraints.MinValue != null && it.NumberAttributeConstraints.MaxValue != null) ||
                    (it.StringAttributeConstraints != null && it.StringAttributeConstraints.MinLength != null && it.StringAttributeConstraints.MaxLength != null))
                    .ToList();

                var sns = stack.IamRoles?.FirstOrDefault(it => it.Id.PreviousArn == item.SmsConfiguration.SnsCallerArn);
                var lambda = stack.Lambdas?.FirstOrDefault(it => it.Id.PreviousArn == lambdaConfig.CustomMessage);

                if (lambda != null)
                    lambdaConfig.CustomMessage = lambda.Id.NewArn;

                if (sns != null && smsConfiguration != null)
                    smsConfiguration.SnsCallerArn = sns.Id.NewArn;

                return new UserPool()
                {
                    Id = new UserPoolId()
                    {
                        PreviousArn = item.Arn,
                        PreviousId = item.Id,
                        PreviousName = item.Name,
                        NewName = NewNameFor(item.Name)
                    },
                    AdminCreateUserConfig = item.AdminCreateUserConfig,
                    AliasAttributes = item.AliasAttributes,
                    AutoVerifiedAttributes = item.AutoVerifiedAttributes,
                    DeviceConfiguration = item.DeviceConfiguration,
                    EmailConfiguration = item.EmailConfiguration,
                    EmailVerificationMessage = item.EmailVerificationMessage,
                    EmailVerificationSubject = item.EmailVerificationSubject,
                    LambdaConfig = lambdaConfig,
                    MfaConfiguration = item.MfaConfiguration,
                    Policies = item.Policies,
                    Schema = schema,
                    SmsAuthenticationMessage = item.SmsAuthenticationMessage,
                    SmsConfiguration = item.SmsConfiguration,
                    SmsVerificationMessage = item.SmsVerificationMessage,
                    UsernameAttributes = item.UsernameAttributes,
                    UserPoolAddOns = item.UserPoolAddOns,
                    VerificationMessageTemplate = item.VerificationMessageTemplate
                };
            }).ToList();
        }

        private async Task<List<UserPoolType>> GetUserPoolsFromSourceAsync(List<UserPoolDescriptionType> collection)
        {
            var resultCollection = new List<UserPoolType>();

            foreach (var item in collection)
            {
                var request = new DescribeUserPoolRequest()
                {
                    UserPoolId = item.Id
                };

                var response = await sourceClient.DescribeUserPoolAsync(request);

                resultCollection.Add(response.UserPool);
            }

            return resultCollection;
        }

        private async Task<List<UserPoolDescriptionType>> ListUserPoolsFromSourceAsync(IResourceNameFilter filter)
        {
            var pools = new List<UserPoolDescriptionType>();

            string token = null;
            do
            {
                var request = new ListUserPoolsRequest()
                {
                    NextToken = token,
                    MaxResults = 60
                };

                var response = await sourceClient.ListUserPoolsAsync(request);

                pools.AddRange(response.UserPools.Where(it => filter.IsValid(it.Name)));

                token = response.NextToken;
            } while (token != null);

            return pools;
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
