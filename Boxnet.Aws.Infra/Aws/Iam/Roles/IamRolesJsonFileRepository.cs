using Boxnet.Aws.Infra.Repositories;
using Boxnet.Aws.Model.Aws.Iam.Roles;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Boxnet.Aws.Infra.Aws.Iam.Roles
{
    public class IamRolesJsonFileRepository : JsonFileRepository<IamRole, IamRoleId>, IIamRolesRepository
    {
        protected override IEnumerable<JsonConverter> Converters
        {
            get { return new JsonConverter[] { new IamRoleJsonConverter() }; }
        }

        public IamRolesJsonFileRepository(string filePath) : base(filePath)
        {
        }
    }
}
