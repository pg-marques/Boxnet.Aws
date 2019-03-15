using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Boxnet.Aws.Mvp.Sns
{
    public class SnsTopicsService : IDisposable
    {
        private const string TopicArnKey = "TopicArn";
        protected readonly AmazonSimpleNotificationServiceClient sourceClient;
        protected readonly AmazonSimpleNotificationServiceClient destinationClient;
        protected readonly Stack stack; protected readonly Dictionary<string, string> tags;

        public SnsTopicsService(
            Stack stack,
            string sourceAccessKey,
            string sourceSecretKey,
            string sourceRegion,
            string destinationAccessKey,
            string destinationSecretKey,
            string destinationRegion)
        {
            this.stack = stack;
            sourceClient = new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials(sourceAccessKey, sourceSecretKey), RegionEndpoint.GetBySystemName(sourceRegion));
            destinationClient = new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials(destinationAccessKey, destinationSecretKey), RegionEndpoint.GetBySystemName(destinationRegion));
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
            var collection = await GetTopicsOnSourceAsync(filter);
            var topicsData = await GetTopicsDataFromSourceAsync(collection);
            var topics = Convert(topicsData);
            await AddSubscriptionsOnSourceAsync(topics);
            await UpdateTopicsWithDestinationData(topics);
            await CreateOnDestinationAsync(topics);
            var z = topicsData.AsReadOnly();
        }

        private async Task CreateOnDestinationAsync(List<SnsTopic> topics)
        {
            foreach(var topic in topics.Where(it => it.Id.NewArn == null).ToList())
            {
                var request = new CreateTopicRequest()
                {
                    Attributes = topic.Attributes,
                    Name = topic.Id.NewName
                };

                var response = await destinationClient.CreateTopicAsync(request);
                topic.Id.NewArn = response.TopicArn;
            }

            foreach(var topic in topics)
            {
                foreach(var subscription in topic.Subscriptions.Where(it => it.Id.NewArn == null).ToList())
                {
                    var request = new SubscribeRequest()
                    {
                        ReturnSubscriptionArn = true,
                        Attributes = subscription.Attributes,
                        Endpoint = subscription.Endpoint,
                        Protocol = subscription.Protocol,
                        TopicArn = subscription.TopicId.NewArn
                    };

                    var response = await destinationClient.SubscribeAsync(request);
                    subscription.Id.NewArn = response.SubscriptionArn;
                }
            }
        }

        private async Task UpdateTopicsWithDestinationData(List<SnsTopic> topics)
        {
            var collection = await GetTopicsOnDestinationAsync();
            var topicsData = await GetTopicsDataFromDestinationAsync(collection);
            var existingTopics = Convert(topicsData);
            await AddSubscriptionsOnDestinationAsync(existingTopics);
            foreach(var topic in topics)
            {
                var existingTopic = existingTopics.FirstOrDefault(it => it.Id.PreviousName == topic.Id.NewName);
                if (existingTopic != null)
                {
                    topic.Id.NewArn = existingTopic.Id.PreviousArn;

                    if (existingTopic.Subscriptions != null && topic.Subscriptions != null)
                        foreach(var subscription in topic.Subscriptions)
                        {
                            var existingSubscription = existingTopic.Subscriptions.FirstOrDefault(it => it.Endpoint == subscription.Endpoint);

                            if (existingSubscription != null)
                                subscription.Id.NewArn = existingSubscription.Id.PreviousArn;
                        }
                }
            }
        }

        private async Task AddSubscriptionsOnDestinationAsync(List<SnsTopic> topics)
        {
            foreach (var topic in topics)
            {
                var subscriptions = new List<Subscription>();

                string token = null;
                do
                {
                    var request = new ListSubscriptionsByTopicRequest()
                    {
                        NextToken = token,
                        TopicArn = topic.Id.PreviousArn
                    };

                    var response = await destinationClient.ListSubscriptionsByTopicAsync(request);
                    subscriptions.AddRange(response.Subscriptions);
                    token = response.NextToken;

                } while (token != null);

                foreach (var subscription in subscriptions.Where(it => it.Protocol == "lambda").ToList())
                {
                    var request = new GetSubscriptionAttributesRequest()
                    {
                        SubscriptionArn = subscription.SubscriptionArn
                    };

                    var response = await destinationClient.GetSubscriptionAttributesAsync(request);

                    var lambda = stack?.Lambdas.FirstOrDefault(it => subscription.Endpoint != null && subscription.Endpoint.Contains(it.Id.PreviousArn));
                    string endpoint = null;
                    if (lambda != null)
                    {
                        endpoint = subscription.Endpoint.Replace(lambda.Id.PreviousArn, lambda.Id.NewArn);
                    }
                    var attributes = new Dictionary<string, string>();

                    if (response.Attributes.ContainsKey("FilterPolicy"))
                        attributes.Add("FilterPolicy", response.Attributes["FilterPolicy"]);

                    if (response.Attributes.ContainsKey("DeliveryPolicy"))
                        attributes.Add("DeliveryPolicy", response.Attributes["DeliveryPolicy"]);

                    topic.Subscriptions.Add(new SnsSubscription()
                    {
                        Endpoint = endpoint,
                        Protocol = subscription.Protocol,
                        TopicId = topic.Id,
                        Id = new ResourceIdWithArn()
                        {
                            PreviousArn = response.Attributes["SubscriptionArn"]
                        },
                        Attributes = attributes
                    });
                }
            }
        }

        private async Task<List<Dictionary<string, string>>> GetTopicsDataFromDestinationAsync(List<Topic> collection)
        {
            var topicsData = new List<Dictionary<string, string>>();

            foreach (var topic in collection)
            {
                var request = new GetTopicAttributesRequest()
                {
                    TopicArn = topic.TopicArn
                };

                var response = await destinationClient.GetTopicAttributesAsync(request);

                topicsData.Add(response.Attributes);
            }

            return topicsData;
        }


        private async Task<List<Topic>> GetTopicsOnDestinationAsync()
        {
            var topics = new List<Topic>();
            var filter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            string token = null;
            do
            {
                var request = new ListTopicsRequest()
                {
                    NextToken = token
                };

                var response = await destinationClient.ListTopicsAsync(request);

                topics.AddRange(response.Topics.Where(it => filter.IsValid(TopicNameFrom(it.TopicArn))).ToList());

                token = response.NextToken;
            } while (token != null);
            return topics;
        }

        private async Task AddSubscriptionsOnSourceAsync(List<SnsTopic> topics)
        {
            foreach (var topic in topics)
            {
                var subscriptions = new List<Subscription>();

                string token = null;
                do
                {
                    var request = new ListSubscriptionsByTopicRequest()
                    {
                        NextToken = token,
                        TopicArn = topic.Id.PreviousArn
                    };

                    var response = await sourceClient.ListSubscriptionsByTopicAsync(request);
                    subscriptions.AddRange(response.Subscriptions);
                    token = response.NextToken;

                } while (token != null);

                foreach(var subscription in subscriptions.Where(it => it.Protocol == "lambda").ToList())
                {
                    var request = new GetSubscriptionAttributesRequest()
                    {
                        SubscriptionArn = subscription.SubscriptionArn
                    };

                    var response = await sourceClient.GetSubscriptionAttributesAsync(request);

                    var lambda = stack?.Lambdas.FirstOrDefault(it => subscription.Endpoint != null && subscription.Endpoint.Contains(it.Id.PreviousArn));
                    string endpoint = null;
                    if (lambda != null)
                    {
                        endpoint = subscription.Endpoint.Replace(lambda.Id.PreviousArn, lambda.Id.NewArn);
                    }
                    var attributes = new Dictionary<string, string>();

                    if (response.Attributes.ContainsKey("FilterPolicy"))
                        attributes.Add("FilterPolicy", response.Attributes["FilterPolicy"]);

                    if (response.Attributes.ContainsKey("DeliveryPolicy"))
                        attributes.Add("DeliveryPolicy", response.Attributes["DeliveryPolicy"]);


                    topic.Subscriptions.Add(new SnsSubscription()
                    {
                        Endpoint = endpoint,
                        Protocol = subscription.Protocol,
                        TopicId  = topic.Id,
                        Id = new ResourceIdWithArn()
                        {
                            PreviousArn = response.Attributes["SubscriptionArn"]
                        },
                        Attributes = attributes
                    });
                }
            }
        }

        private List<SnsTopic> Convert(List<Dictionary<string, string>> topicsData)
        {
            var topics = new List<SnsTopic>();

            foreach (var item in topicsData)
            {
                var attributes = new Dictionary<string, string>();

                if (item.ContainsKey("Policy") && item["Policy"] != null)
                    attributes.Add("Policy", item["Policy"].Replace(TopicNameFrom(item[TopicArnKey]), NewNameFor(TopicNameFrom(item[TopicArnKey]))));

                if (item.ContainsKey("DisplayName") && item["DisplayName"] != null)
                    attributes.Add("DisplayName", item["DisplayName"]);

                if (item.ContainsKey("EffectiveDeliveryPolicy") && item["EffectiveDeliveryPolicy"] != null)
                    attributes.Add("DeliveryPolicy", item["EffectiveDeliveryPolicy"]);


                topics.Add(new SnsTopic()
                {
                    Id = new ResourceIdWithArn()
                    {
                        NewName = NewNameFor(TopicNameFrom(item[TopicArnKey])),
                        PreviousName = TopicNameFrom(item[TopicArnKey]),
                        PreviousArn = item[TopicArnKey]
                    },
                    Attributes = attributes
                });

            }

            return topics;
        }

        protected string NewNameFor(string name)
        {
            if (name.StartsWith(stack.Name))
                name = string.Join("", name.Split('_').Skip(1));

            return string.Format("{0}{1}", Prefix(), name);
        }

        protected string Prefix()
        {
            return string.Format("{0}{1}_", stack.Name, stack.Environment);
        }

        private async Task<List<Dictionary<string, string>>> GetTopicsDataFromSourceAsync(List<Topic> collection)
        {
            var topicsData = new List<Dictionary<string, string>>();

            foreach (var topic in collection)
            {
                var request = new GetTopicAttributesRequest()
                {
                    TopicArn = topic.TopicArn
                };

                var response = await sourceClient.GetTopicAttributesAsync(request);

                topicsData.Add(response.Attributes);
            }

            return topicsData;
        }

        private string TopicNameFrom(string arn)
        {
            if (string.IsNullOrWhiteSpace(arn))
                return string.Empty;

            return arn.Split(':').LastOrDefault();
        }

        private async Task<List<Topic>> GetTopicsOnSourceAsync(IResourceNameFilter filter)
        {
            var topics = new List<Topic>();
            string token = null;
            do
            {
                var request = new ListTopicsRequest()
                {
                    NextToken = token
                };

                var response = await sourceClient.ListTopicsAsync(request);

                topics.AddRange(response.Topics.Where(it => filter.IsValid(TopicNameFrom(it.TopicArn))).ToList());

                token = response.NextToken;
            } while (token != null);
            return topics;
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
