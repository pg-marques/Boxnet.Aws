using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp
{
    public class ResourceIdWithAwsId : ResourceId
    {
        public string PreviousId { get; set; }
        public string NewId { get; set; }
    }
}
