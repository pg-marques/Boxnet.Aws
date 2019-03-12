using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Boxnet.Aws.Mvp.Apis
{
    public class AwsApiResource
    {
        public ResourceId Id { get; set; }
        public ResourceId ParentId { get; set; }
        public string PathPart { get; set; }
        public string Path { get; set; }
        public ResourceIdWithAwsId RestApiId { get; set; }
        public List<AwsApiResource> Children { get; set; } = new List<AwsApiResource>();
        public List<AwsApiMethod> Methods { get; set; } = new List<AwsApiMethod>();
    }
}
