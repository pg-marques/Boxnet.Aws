using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public interface IIamAttachablePoliciesRepository
    {
        Task AddAsync(IamAttachablePolicy policy);
        Task SaveAsync(IamAttachablePolicy policy);
        Task DeleteAsync(IamAttachablePolicy policy);
        IamAttachablePolicy ByIdAsync(IamAttachablePolicyId id);
        Task<IEnumerable<IamAttachablePolicy>> ByAsync();
    }
}
