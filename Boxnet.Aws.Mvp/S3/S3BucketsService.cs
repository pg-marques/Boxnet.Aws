using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Boxnet.Aws.Mvp.S3
{
    public class S3BucketsService : IDisposable
    {
        private const string TopicArnKey = "TopicArn";
        protected readonly AmazonS3Client sourceClient;
        protected readonly AmazonS3Client destinationClient;
        protected readonly Stack stack; protected readonly Dictionary<string, string> tags;
        private readonly string destinationRegion;

        public S3BucketsService(
            Stack stack,
            string sourceAccessKey,
            string sourceSecretKey,
            string sourceRegion,
            string destinationAccessKey,
            string destinationSecretKey,
            string destinationRegion)
        {
            this.stack = stack;
            sourceClient = new AmazonS3Client(new BasicAWSCredentials(sourceAccessKey, sourceSecretKey), RegionEndpoint.GetBySystemName(sourceRegion));
            destinationClient = new AmazonS3Client(new BasicAWSCredentials(destinationAccessKey, destinationSecretKey), RegionEndpoint.GetBySystemName(destinationRegion));
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
            this.destinationRegion = destinationRegion;
        }

        protected string NewNameFor(string name)
        {
            if (name.StartsWith(stack.Name))
                name = string.Join("", name.Split('-').Skip(1));

            return string.Format("{0}{1}", Prefix(), name);
        }

        protected string Prefix()
        {
            return string.Format("{0}.{1}-", stack.Name.ToLower(), stack.Environment.ToLower());
        }

        public async Task CopyAsync(IResourceNameFilter filter)
        {

            var collection = await ListBucketsOnSourceNotWebAsync(filter);
            var buckets = Convert(collection);
            var existingCollection = await ListBucketsOndestinationNotWebAsync();
            //
            foreach (var bucket in buckets)//.Where(it => !existingCollection.Any(e => e == it.Id.NewName)).ToList())
            {
                //var request = new PutBucketRequest()
                //{
                //    BucketName = bucket.Id.NewName,
                //    Grants = bucket.ACL.Grants,
                //    ObjectLockEnabledForBucket = false,
                //    UseClientRegion = true,
                //};

                //var response = await destinationClient.PutBucketAsync(request);

                //if (bucket.BucketWebsiteConfiguration?.IndexDocumentSuffix != null && bucket.BucketWebsiteConfiguration?.ErrorDocument != null)
                //{
                //    var websiteRequest = new PutBucketWebsiteRequest()
                //    {
                //        BucketName = bucket.Id.NewName,
                //        WebsiteConfiguration = bucket.BucketWebsiteConfiguration
                //    };

                //    var websiteResponse = await destinationClient.PutBucketWebsiteAsync(websiteRequest);
                //}

                //if (bucket.LambdaFunctionConfigurations != null && bucket.LambdaFunctionConfigurations?.Count() > 0)
                //{
                //    var notificationRequest = new PutBucketNotificationRequest()
                //    {
                //        BucketName = bucket.Id.NewName,
                //        LambdaFunctionConfigurations = bucket.LambdaFunctionConfigurations,
                //    };

                //    var notificationResponse = await destinationClient.PutBucketNotificationAsync(notificationRequest);
                //}

                //if (!string.IsNullOrWhiteSpace(bucket?.Policy))
                //{
                //    var policyRequest = new PutBucketPolicyRequest()
                //    {
                //        BucketName = bucket.Id.NewName,
                //        Policy = bucket.Policy
                //    };

                //    var policyResponse = await destinationClient.PutBucketPolicyAsync(policyRequest);
                //}

            }


            stack.Buckets = buckets;
        }

        private List<AwsS3Bucket> Convert(List<S3Wrapper> collection)
        {
            var buckets = new List<AwsS3Bucket>();
            foreach (var bucket in collection)
            {
                WebsiteConfiguration websiteConfiguration = null;

                if (bucket.GetBucketWebsiteResponse?.WebsiteConfiguration != null)
                {
                    websiteConfiguration = new WebsiteConfiguration()
                    {
                        ErrorDocument = bucket.GetBucketWebsiteResponse?.WebsiteConfiguration.ErrorDocument,
                        IndexDocumentSuffix = bucket.GetBucketWebsiteResponse?.WebsiteConfiguration.IndexDocumentSuffix,
                        RedirectAllRequestsTo = bucket.GetBucketWebsiteResponse?.WebsiteConfiguration.RedirectAllRequestsTo,
                        RoutingRules = bucket.GetBucketWebsiteResponse?.WebsiteConfiguration?.RoutingRules?.Select(it => new RoutingRule()
                        {
                            Condition = new RoutingRuleCondition()
                            {
                                HttpErrorCodeReturnedEquals = it.Condition?.HttpErrorCodeReturnedEquals,
                                KeyPrefixEquals = it.Condition?.KeyPrefixEquals?.Replace(bucket.BucketName, NewNameFor(bucket.BucketName))
                            },
                            Redirect = new RoutingRuleRedirect()
                            {
                                HostName = it.Redirect?.HostName?.Replace(bucket.BucketName, NewNameFor(bucket.BucketName)),
                                HttpRedirectCode = it.Redirect?.HttpRedirectCode,
                                Protocol = it.Redirect?.Protocol,
                                ReplaceKeyPrefixWith = it.Redirect?.ReplaceKeyPrefixWith,
                                ReplaceKeyWith = it.Redirect?.ReplaceKeyWith
                            }
                        }).ToList()
                    };
                }

                List<LambdaFunctionConfiguration> lambdasConfigurations = null;

                if (bucket.GetBucketNotificationResponse?.LambdaFunctionConfigurations != null && bucket.GetBucketNotificationResponse.LambdaFunctionConfigurations.Count() > 0)
                {
                    lambdasConfigurations = bucket.GetBucketNotificationResponse.LambdaFunctionConfigurations.Select(it =>
                    {
                        var lambda = stack.Lambdas?.FirstOrDefault(l => l.Id.PreviousArn == it.FunctionArn);
                        var arn = it.FunctionArn;
                        if (lambda != null)
                            arn = lambda.Id.NewArn;

                        return new LambdaFunctionConfiguration()
                        {
                            Events = it.Events,
                            Filter = it.Filter,
                            FunctionArn = arn,
                            Id = it.Id
                        };
                    }).ToList();
                }

                buckets.Add(new AwsS3Bucket()
                {
                    Id = new ResourceId()
                    {
                        PreviousName = bucket.BucketName,
                        NewName = NewNameFor(bucket.BucketName),
                    },
                    ACL = bucket.GetACLResponse?.AccessControlList,
                    BucketWebsiteConfiguration = websiteConfiguration,
                    CORSConfiguration = bucket.GetCORSConfigurationResponse?.Configuration,
                    LambdaFunctionConfigurations = lambdasConfigurations,
                    Policy = bucket.GetBucketPolicyResponse?.Policy?.Replace(bucket.BucketName, NewNameFor(bucket.BucketName)),
                    QueueConfigurations = bucket.GetBucketNotificationResponse?.QueueConfigurations,
                    S3BucketVersioningConfig = bucket.GetBucketVersioningResponse?.VersioningConfig,
                    TopicConfigurations = bucket.GetBucketNotificationResponse?.TopicConfigurations
                });
            }

            return buckets;
        }

        private async Task<List<string>> ListBucketsOndestinationNotWebAsync()
        {
            var filter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            var request = new ListBucketsRequest();
            var response = await destinationClient.ListBucketsAsync(request);

            return response.Buckets.Where(it => filter.IsValid(it.BucketName)).Select(it => it.BucketName).ToList(); ;
        }

        private async Task<List<S3Wrapper>> ListBucketsOnSourceNotWebAsync(IResourceNameFilter filter)
        {
            var request = new ListBucketsRequest();
            var response = await sourceClient.ListBucketsAsync(request);

            var buckets = response.Buckets.Where(it => filter.IsValid(it.BucketName)).Select(it => new S3Wrapper()
            {
                BucketName = it.BucketName
            }).ToList(); ;

            foreach (var bucket in buckets)
            {
                var detailsRequest = new GetBucketVersioningRequest()
                {
                    BucketName = bucket.BucketName
                };

                var detailsResponse = await sourceClient.GetBucketVersioningAsync(detailsRequest);
                bucket.GetBucketVersioningResponse = detailsResponse;

            }

            foreach (var bucket in buckets)
            {
                var detailsRequest = new GetBucketWebsiteRequest()
                {
                    BucketName = bucket.BucketName
                };

                var detailsResponse = await sourceClient.GetBucketWebsiteAsync(detailsRequest);
                bucket.GetBucketWebsiteResponse = detailsResponse;
            }

            foreach (var bucket in buckets)
            {
                var detailsRequest = new GetBucketNotificationRequest()
                {
                    BucketName = bucket.BucketName
                };

                var detailsResponse = await sourceClient.GetBucketNotificationAsync(detailsRequest);
                bucket.GetBucketNotificationResponse = detailsResponse;
            }

            foreach (var bucket in buckets)
            {
                var detailsRequest = new GetBucketPolicyRequest()
                {
                    BucketName = bucket.BucketName
                };

                var detailsResponse = await sourceClient.GetBucketPolicyAsync(detailsRequest);
                bucket.GetBucketPolicyResponse = detailsResponse;
            }

            foreach (var bucket in buckets)
            {
                var detailsRequest = new GetACLRequest()
                {
                    BucketName = bucket.BucketName
                };

                var detailsResponse = await sourceClient.GetACLAsync(detailsRequest);
                bucket.GetACLResponse = detailsResponse;
            }

            foreach (var bucket in buckets)
            {
                var detailsRequest = new GetCORSConfigurationRequest()
                {
                    BucketName = bucket.BucketName
                };

                var detailsResponse = await sourceClient.GetCORSConfigurationAsync(detailsRequest);
                bucket.GetCORSConfigurationResponse = detailsResponse;
            }

            return buckets;
        }

        private class S3Wrapper
        {
            public string BucketName { get; set; }
            public GetBucketVersioningResponse GetBucketVersioningResponse { get; set; }
            public GetBucketWebsiteResponse GetBucketWebsiteResponse { get; set; }
            public GetBucketNotificationResponse GetBucketNotificationResponse { get; set; }
            public GetBucketPolicyResponse GetBucketPolicyResponse { get; set; }
            public GetACLResponse GetACLResponse { get; set; }
            public GetCORSConfigurationResponse GetCORSConfigurationResponse { get; set; }
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
