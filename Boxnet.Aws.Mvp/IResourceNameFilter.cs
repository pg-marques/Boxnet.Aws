using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp
{
    public interface IResourceNameFilter
    {
        bool IsValid(string resourceName);
    }
}
