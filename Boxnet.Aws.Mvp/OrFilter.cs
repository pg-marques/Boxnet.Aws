using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp
{
    public class OrFilter : IResourceNameFilter
    {
        private readonly IEnumerable<IResourceNameFilter> filters;
        public OrFilter(params IResourceNameFilter[] filters)
        {
            this.filters = filters;
        }

        public bool IsValid(string resourceName)
        {
            foreach (var filter in filters)
                if (filter.IsValid(resourceName))
                    return true;

            return false;
        }
    }
}
