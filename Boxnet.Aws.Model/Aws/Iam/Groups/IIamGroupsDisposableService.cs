using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.Model.Aws.Iam.Groups
{
    public interface IIamGroupsDisposableService : IDisposable
    {
        Task<IEnumerable<IamGroup>> ListByFilterAsync(IResourceIdFilter filter);
        Task CreateAsync(IamGroup group);
        Task DeleteAsync(IamGroup group);
        Task AttachPoliciesAsync(IamGroup group);
        Task DetachPoliciesAsync(IamGroup group);
    }
}
