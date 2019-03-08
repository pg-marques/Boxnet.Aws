﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp
{
    public class ResourceNameStarsWithTermInsensitiveCaseFilter : IResourceNameFilter
    {
        private readonly string term;

        public ResourceNameStarsWithTermInsensitiveCaseFilter(string term)
        {
            this.term = term;
        }

        public bool IsValid(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                return false;

            return resourceName.ToLower().StartsWith(term.ToLower());
        }
    }
}
