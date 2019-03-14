using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp
{
    public class EqualsCaseInsensitiveFilter : IResourceNameFilter
    {
        private readonly string term;

        public EqualsCaseInsensitiveFilter(string term)
        {
            this.term = term;
        }

        public bool IsValid(string resourceName)
        {
            return resourceName == term;
        }
    }
}
