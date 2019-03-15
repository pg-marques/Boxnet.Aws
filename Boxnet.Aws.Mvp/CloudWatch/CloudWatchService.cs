using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using System.Linq;

namespace Boxnet.Aws.Mvp.CloudWatch
{
    public class CloudWatchService : IDisposable
    {
        private const string TopicArnKey = "TopicArn";
        protected readonly AmazonCloudWatchEventsClient sourceClient;
        protected readonly AmazonCloudWatchEventsClient destinationClient;
        protected readonly Stack stack; protected readonly Dictionary<string, string> tags;

        public CloudWatchService(
            Stack stack,
            string sourceAccessKey,
            string sourceSecretKey,
            string sourceRegion,
            string destinationAccessKey,
            string destinationSecretKey,
            string destinationRegion)
        {
            this.stack = stack;
            sourceClient = new AmazonCloudWatchEventsClient(new BasicAWSCredentials(sourceAccessKey, sourceSecretKey), RegionEndpoint.GetBySystemName(sourceRegion));
            destinationClient = new AmazonCloudWatchEventsClient(new BasicAWSCredentials(destinationAccessKey, destinationSecretKey), RegionEndpoint.GetBySystemName(destinationRegion));
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
            var collection = await ListRulesOnSourceAsync(filter);
            var rules = Convert(collection);
            var existingCollection = await ListRulesOnDestinationAsync();
            foreach(var rule in rules)
            {
                var existingRule = existingCollection.FirstOrDefault(it => it.Name == rule.Id.NewName);
                if (existingRule != null)
                    rule.Id.NewArn = existingRule.Arn;
            }


            foreach (var rule in rules.Where(it => it.Id.NewArn == null).ToList())
            {
                var request = new PutRuleRequest()
                {
                    Description = rule.Description,
                    EventPattern = rule.EventPattern,
                    Name = rule.Id.NewName,
                    RoleArn = null,
                    ScheduleExpression = rule.ScheduleExpression,
                    State = rule.State
                };

                var response = await destinationClient.PutRuleAsync(request);
                rule.Id.NewArn = response.RuleArn;

                string token = null;
                do
                {
                    var targetsRequest = new ListTargetsByRuleRequest()
                    {
                        Rule = rule.Id.PreviousName,
                        NextToken = token
                    };

                    var targetResponse = await sourceClient.ListTargetsByRuleAsync(targetsRequest);

                    var putTargetRequest = new PutTargetsRequest()
                    {
                        Rule = rule.Id.NewName,
                        Targets = targetResponse.Targets.Select(it =>
                        {
                            var lambda = stack.Lambdas.FirstOrDefault(item => item.Id.PreviousArn == it.Arn);
                            if (lambda == null)
                                return null;

                            return new Target()
                            {
                                Arn = lambda.Id.NewArn,
                                Input = it.Input,
                                InputPath = it.InputPath,
                                BatchParameters = it.BatchParameters,
                                InputTransformer = it.InputTransformer,
                                KinesisParameters = it.KinesisParameters,
                                EcsParameters = it.EcsParameters,
                                RunCommandParameters = it.RunCommandParameters,
                                SqsParameters = it.SqsParameters,
                                Id = Guid.NewGuid().ToString("N")
                            };
                        }).Where(it => it != null).ToList(),

                    };

                    await destinationClient.PutTargetsAsync(putTargetRequest);

                    token = targetResponse.NextToken;

                } while (token != null);


            }

            stack.CloudWatchRules = rules;
        }

        private List<CloudWatchRule> Convert(List<DescribeRuleResponse> collection)
        {
            var rules = new List<CloudWatchRule>();

            foreach (var rule in collection)
            {
                rules.Add(new CloudWatchRule()
                {
                    Id = new ResourceIdWithArn()
                    {
                        PreviousName = rule.Name,
                        PreviousArn = rule.Arn,
                        NewName = NewNameFor(rule.Name)
                    },
                    Description = rule.Description,
                    EventPattern = rule.EventPattern,
                    ScheduleExpression = rule.ScheduleExpression,
                    State = rule.State
                });
            }

            return rules;
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

        private async Task<List<DescribeRuleResponse>> ListRulesOnSourceAsync(IResourceNameFilter filter)
        {
            var rules = new List<Rule>();
            var responses = new List<DescribeRuleResponse>();
            string token = null;
            do
            {
                var request = new ListRulesRequest()
                {
                    NextToken = token
                };

                var response = await sourceClient.ListRulesAsync(request);

                rules.AddRange(response.Rules.Where(it => filter.IsValid(it.Name)).ToList());

                token = response.NextToken;

            } while (token != null);

            foreach (var rule in rules)
            {
                var request = new DescribeRuleRequest()
                {
                    Name = rule.Name
                };

                var response = await sourceClient.DescribeRuleAsync(request);

                responses.Add(response);
            }

            return responses;
        }

        private async Task<List<DescribeRuleResponse>> ListRulesOnDestinationAsync()
        {
            var filter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            var rules = new List<Rule>();
            var responses = new List<DescribeRuleResponse>();
            string token = null;
            do
            {
                var request = new ListRulesRequest()
                {
                    NextToken = token
                };

                var response = await destinationClient.ListRulesAsync(request);

                rules.AddRange(response.Rules.Where(it => filter.IsValid(it.Name)).ToList());

                token = response.NextToken;

            } while (token != null);

            foreach (var rule in rules)
            {
                var request = new DescribeRuleRequest()
                {
                    Name = rule.Name
                };

                var response = await destinationClient.DescribeRuleAsync(request);

                responses.Add(response);
            }

            return responses;
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
