using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public interface IIamRolesDisposableService : IDisposable
    {
        Task<IEnumerable<IamRole>> ListByFilterAsync(IResourceIdFilter filter);
        Task CreateAsync(IamRole role);
        Task DeleteAsync(IamRole role);
    }
}
