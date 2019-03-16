using Boxnet.Aws.Mvp.Apis;
using Boxnet.Aws.Mvp.CloudWatch;
using Boxnet.Aws.Mvp.Cognito;
using Boxnet.Aws.Mvp.Iam;
using Boxnet.Aws.Mvp.Lambdas;
using Boxnet.Aws.Mvp.Newtworking;
using Boxnet.Aws.Mvp.S3;
using Boxnet.Aws.Mvp.Sns;
using Boxnet.Aws.Mvp.Sqs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp
{
    public class Stack
    {
        public string Name { get; set; }
        public string Environment { get; set; }
        public IEnumerable<IamPolicy> IamPolicies { get; set; }
        public IEnumerable<IamRole> IamRoles { get; set; }
        public IEnumerable<IamGroup> IamGroups { get; set; }
        public IEnumerable<IamUser> IamUsers { get; set; }
        public IEnumerable<AwsVpc> Vpcs { get; set; }
        public IEnumerable<Lambda> Lambdas { get; set; }
        public IEnumerable<UserPool> UsersPools { get; set; }
        public IEnumerable<AwsApi> Apis { get; set; }
        public IEnumerable<SnsTopic> SnsTopics { get; set; }
        public IEnumerable<SqsQueue> SqsQueues { get; set; }
        public IEnumerable<CloudWatchRule> CloudWatchRules { get; set; }
        public IEnumerable<AwsS3Bucket> Buckets { get; set; }
    }
}
