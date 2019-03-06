﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public interface IIamUsersRepository
    {
        Task AddAsync(IamUser user);
        Task SaveAsync(IamUser user);
        Task DeleteAsync(IamUser user);
        Task<IamUser> ByAsync(IamUserId id);
        Task<IEnumerable<IamUser>> AllAsync();
    }
}
