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
        public ResourceIdWithAwsId RestApiId { get; set; }
        public IEnumerable<string> Levels
        {
            get
            {
                return PathPart?.Trim('/').Split('/');
            }
        }

        public int Depth
        {
            get
            {
                return Levels == null || Levels.Count() < 1 ? 0 : Levels.Count();
            }
        }
    }
}
