using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.Model.Aws.Iam.Roles
{
    public interface IIamRolesRepository
    {
        Task AddAsync(IamRole role);
        Task SaveAsync(IamRole role);
        Task DeleteAsync(IamRole role);
        Task<IamRole> ByAsync(IamRoleId id);
        Task<IEnumerable<IamRole>> AllAsync();
    }
}
