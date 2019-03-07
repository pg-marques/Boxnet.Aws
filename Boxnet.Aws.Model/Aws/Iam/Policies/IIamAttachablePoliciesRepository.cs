using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.Model.Aws.Iam.Policies
{
    public interface IIamAttachablePoliciesRepository
    {
        Task AddAsync(IamAttachablePolicy policy);
        Task SaveAsync(IamAttachablePolicy policy);
        Task DeleteAsync(IamAttachablePolicy policy);
        Task<IamAttachablePolicy> ByAsync(IamAttachablePolicyId id);
        Task<IEnumerable<IamAttachablePolicy>> AllAsync();
    }
}
