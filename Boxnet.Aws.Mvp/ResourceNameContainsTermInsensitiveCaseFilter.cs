using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp
{
    public class ResourceNameContainsTermInsensitiveCaseFilter : IResourceNameFilter
    {
        private readonly string term;

        public ResourceNameContainsTermInsensitiveCaseFilter(string term)
        {
            this.term = term;
        }

        public bool IsValid(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                return false;

            return resourceName.ToLower().Contains(term.ToLower());
        }
    }
}
