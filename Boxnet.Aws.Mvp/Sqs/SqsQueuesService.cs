using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Boxnet.Aws.Mvp.Sqs
{
    public class SqsQueuesService : IDisposable
    {
        private const string TopicArnKey = "TopicArn";
        protected readonly AmazonSQSClient sourceClient;
        protected readonly AmazonSQSClient destinationClient;
        protected readonly Stack stack; protected readonly Dictionary<string, string> tags;

        public SqsQueuesService(
            Stack stack,
            string sourceAccessKey,
            string sourceSecretKey,
            string sourceRegion,
            string destinationAccessKey,
            string destinationSecretKey,
            string destinationRegion)
        {
            this.stack = stack;
            sourceClient = new AmazonSQSClient(new BasicAWSCredentials(sourceAccessKey, sourceSecretKey), RegionEndpoint.GetBySystemName(sourceRegion));
            destinationClient = new AmazonSQSClient(new BasicAWSCredentials(destinationAccessKey, destinationSecretKey), RegionEndpoint.GetBySystemName(destinationRegion));
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
            var queuesData = await GetQueuesDataOnSourceAsync(filter);
            var queues = Convert(queuesData);
            await UpdateWithDataFromDestinationAsync(queues);
            await CreateOnDestinationAsync(queues);
        }

        private async Task CreateOnDestinationAsync(List<SqsQueue> queues)
        {
            foreach(var queue in queues.Where(it => it.Id.NewArn == null).ToList())
            {
                var request = new CreateQueueRequest()
                {
                    Attributes = queue.Attributes,
                    QueueName = queue.Id.NewName
                };

                var response = await destinationClient.CreateQueueAsync(request);
                queue.Id.NewArn = response.QueueUrl;
            }
        }

        private async Task UpdateWithDataFromDestinationAsync(List<SqsQueue> queues)
        {
            var queuesData = await GetQueuesDataOnDestinationAsync();
            var existingQueues = Convert(queuesData);
            foreach(var queue in queues)
            {
                var existingQueue = existingQueues.FirstOrDefault(it => it.Id.PreviousName == queue.Id.NewName);
                if (existingQueue != null)
                    queue.Id.NewArn = existingQueue.Id.PreviousArn;
            }
        }
        private async Task<List<GetQueueAttributesResponse>> GetQueuesDataOnDestinationAsync()
        {
            var filter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            var queuesData = new List<GetQueueAttributesResponse>();
            var request = new ListQueuesRequest();
            var response = await sourceClient.ListQueuesAsync(request);
            foreach (var url in response.QueueUrls)
            {
                var urlRequest = new GetQueueAttributesRequest()
                {
                    AttributeNames = new List<string>() { "All" },
                    QueueUrl = url
                };

                var urlResponse = await destinationClient.GetQueueAttributesAsync(urlRequest);
                var name = QueueNameFrom(urlResponse.QueueARN);
                if (filter.IsValid(name))
                    queuesData.Add(urlResponse);
            }

            return queuesData;
        }

        private List<SqsQueue> Convert(List<GetQueueAttributesResponse> queuesData)
        {
            return queuesData?.Select(it =>
            {
                var attributes = new Dictionary<string, string>();

                foreach (var attribute in it.Attributes)
                {
                    if (attribute.Key.ToLower() == "policy")
                    {
                        var policy = attribute.Value;
                        if (policy != null && policy.Contains(it.QueueARN))
                            policy = policy.Replace(QueueNameFrom(it.QueueARN), NewNameFor(QueueNameFrom(it.QueueARN)));

                        attributes.Add(attribute.Key, policy);
                    }
                    else if (!new string[] 
                    {
                        "ApproximateNumberOfMessagesNotVisible",
                        "ApproximateNumberOfMessages",
                        "ApproximateNumberOfMessagesDelayed",
                        "QueueArn", "CreatedTimestamp",
                        "LastModifiedTimestamp"
                    }.Any(invalidKey => invalidKey.ToLower() == attribute.Key.ToLower()))
                    {
                        attributes.Add(attribute.Key, attribute.Value);
                    }
                }

                return new SqsQueue()
                {
                    Id = new ResourceIdWithArn()
                    {
                        PreviousArn = it.QueueARN,
                        PreviousName = QueueNameFrom(it.QueueARN),
                        NewName = NewNameFor(QueueNameFrom(it.QueueARN))
                    },
                    Attributes = attributes
                };
            }).ToList();


        }

        private async Task<List<GetQueueAttributesResponse>> GetQueuesDataOnSourceAsync(IResourceNameFilter filter)
        {
            var queuesData = new List<GetQueueAttributesResponse>();
            var request = new ListQueuesRequest();
            var response = await sourceClient.ListQueuesAsync(request);
            foreach (var url in response.QueueUrls)
            {
                var urlRequest = new GetQueueAttributesRequest()
                {
                    AttributeNames = new List<string>() { "All" },
                    QueueUrl = url
                };

                var urlResponse = await sourceClient.GetQueueAttributesAsync(urlRequest);
                var name = QueueNameFrom(urlResponse.QueueARN);
                if (filter.IsValid(name))
                    queuesData.Add(urlResponse);
            }

            return queuesData;
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

        private string QueueNameFrom(string arn)
        {
            if (string.IsNullOrWhiteSpace(arn))
                return string.Empty;

            return arn.Split(':').LastOrDefault();
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