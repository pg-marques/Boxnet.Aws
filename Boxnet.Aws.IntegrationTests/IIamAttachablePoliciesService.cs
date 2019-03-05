using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public interface IIamAttachablePoliciesService : IDisposable
    {
        Task<IEnumerable<IamAttachablePolicy>> ListByFilterAsync(IResourceIdFilter filter);
        Task CreateAsync(IamAttachablePolicy policy);
        Task DeleteAsync(IamAttachablePolicy policy);
    }
}
