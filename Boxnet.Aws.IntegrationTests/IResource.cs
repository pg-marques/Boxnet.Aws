using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.IntegrationTests
{
    public interface IResource<TResourceId> where TResourceId : IResourceId
    {
        TResourceId ResourceId { get; }
    }
}
