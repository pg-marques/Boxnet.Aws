using Amazon.IdentityManagement.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Boxnet.Aws.IntegrationTests
{
    public class RolesDetails : IEnumerable<RoleDetail>
    {
        private readonly IEnumerable<RoleDetail> details = new List<RoleDetail>();

        public RolesDetails(IEnumerable<RoleDetail> details)
        {
            this.details = details;
        }

        public RolesDetails FilterBy(IResourceIdFilter filter)
        {
            return new RolesDetails(details.Where(role => filter.IsSatisfiedBy(new IamRoleResourceId(role.RoleName))));
        }

        public IEnumerator<RoleDetail> GetEnumerator()
        {
            return details.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return details.GetEnumerator();
        }
    }
}
