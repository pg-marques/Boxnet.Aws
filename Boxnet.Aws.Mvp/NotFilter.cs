using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp
{
    public class NotFilter : IResourceNameFilter
    {
        private readonly IResourceNameFilter filter;

        public NotFilter(IResourceNameFilter filter)
        {
            this.filter = filter;
        }

        public bool IsValid(string resourceName)
        {
            return !filter.IsValid(resourceName);
        }
    }
}
