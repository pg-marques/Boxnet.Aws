using Newtonsoft.Json;
using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamAttachablePoliciesJsonFileRepository : JsonFileRepository<IamAttachablePolicy, IamAttachablePolicyId>, IIamAttachablePoliciesRepository
    {
        protected override IEnumerable<JsonConverter> Converters
        {
            get { return new JsonConverter[] { new IamAttachablePolicyJsonConverter() }; }
        }

        public IamAttachablePoliciesJsonFileRepository(string filePath) : base(filePath)
        {
        }
    }
}
