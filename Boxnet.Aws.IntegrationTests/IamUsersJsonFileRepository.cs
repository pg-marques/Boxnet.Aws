using Newtonsoft.Json;
using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamUsersJsonFileRepository : JsonFileRepository<IamUser, IamUserId>, IIamUsersRepository
    {
        protected override IEnumerable<JsonConverter> Converters
        {
            get { return new JsonConverter[] { new IamUserJsonConverter() }; }
        }

        public IamUsersJsonFileRepository(string filePath) : base(filePath)
        {
        }
    }
}
