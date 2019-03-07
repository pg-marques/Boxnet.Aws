using Boxnet.Aws.Infra.Repositories;
using Boxnet.Aws.Model.Aws.Iam.Users;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Boxnet.Aws.Infra.Aws.Iam.Users
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
