using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Apis
{
    public class AwsApiValidator
    {
        public ResourceIdWithAwsId Id { get; set; }
        public ResourceIdWithAwsId ApiId { get; set; }
        public bool ValidateRequestBody { get; set; }
        public bool ValidateRequestParameters { get; set; }
    }
}
