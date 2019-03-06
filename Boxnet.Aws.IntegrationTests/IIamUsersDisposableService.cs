using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public interface IIamUsersDisposableService : IDisposable
    {
        Task<IEnumerable<IamUser>> ListByFilterAsync(IResourceIdFilter filter);
        Task CreateAsync(IamUser user);
        Task DeleteAsync(IamUser user);
        Task AddToGroupsAsync(IamUser user);
        Task RemoveFromGroupsAsync(IamUser user);
    }
}
