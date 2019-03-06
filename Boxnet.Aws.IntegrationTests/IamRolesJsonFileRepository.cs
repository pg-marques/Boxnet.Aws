using Newtonsoft.Json;
using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
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
