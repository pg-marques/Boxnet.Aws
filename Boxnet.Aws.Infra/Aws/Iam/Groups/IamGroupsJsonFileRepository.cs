using Boxnet.Aws.Infra.Repositories;
using Boxnet.Aws.Model.Aws.Iam.Groups;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Boxnet.Aws.Infra.Aws.Iam.Groups
{
    public class IamGroupsJsonFileRepository : JsonFileRepository<IamGroup, IamGroupId>, IIamGroupsRepository
    {
        protected override IEnumerable<JsonConverter> Converters
        {
            get { return new JsonConverter[] { new IamGroupJsonConverter() }; }
        }

        public IamGroupsJsonFileRepository(string filePath) : base(filePath)
        {
        }
    }
}
