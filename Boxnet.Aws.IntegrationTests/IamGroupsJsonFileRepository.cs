using System.Collections.Generic;
using Newtonsoft.Json;

namespace Boxnet.Aws.IntegrationTests
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
