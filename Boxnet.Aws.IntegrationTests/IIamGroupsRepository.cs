using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public interface IIamGroupsRepository
    {
        Task AddAsync(IamGroup policy);
        Task SaveAsync(IamGroup IamRole);
        Task DeleteAsync(IamGroup policy);
        Task<IamGroup> ByAsync(IamGroupId id);
        Task<IEnumerable<IamGroup>> AllAsync();
    }
}
