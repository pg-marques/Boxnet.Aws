using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public interface IIamRolesRepository
    {
        Task AddAsync(IamRole policy);
        Task SaveAsync(IamRole IamRole);
        Task DeleteAsync(IamRole policy);
        Task<IamRole> ByAsync(IamRoleId id);
        Task<IEnumerable<IamRole>> AllAsync();
    }
}
