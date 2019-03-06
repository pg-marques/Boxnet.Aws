using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public interface IIamGroupsRepository
    {
        Task AddAsync(IamGroup group);
        Task SaveAsync(IamGroup group);
        Task DeleteAsync(IamGroup group);
        Task<IamGroup> ByAsync(IamGroupId id);
        Task<IEnumerable<IamGroup>> AllAsync();
    }
}
