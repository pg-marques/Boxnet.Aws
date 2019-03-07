using Boxnet.Aws.Infra.Repositories;
using Boxnet.Aws.Model.Aws.Iam.Policies;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Boxnet.Aws.Infra.Aws.Iam.Policies
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
