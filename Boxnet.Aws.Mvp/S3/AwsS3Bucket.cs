using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.S3
{
    public class AwsS3Bucket
    {
        public ResourceId Id { get; set; }
        public S3BucketVersioningConfig S3BucketVersioningConfig { get; set; }
        public S3AccessControlList ACL { get; set; }
        public WebsiteConfiguration BucketWebsiteConfiguration { get; set; }
        public List<TopicConfiguration> TopicConfigurations { get; set; }
        public List<QueueConfiguration> QueueConfigurations { get; set; }
        public List<LambdaFunctionConfiguration> LambdaFunctionConfigurations { get; set; }
        public string Policy { get; set; }
        public CORSConfiguration CORSConfiguration { get; set; }
    }
}
