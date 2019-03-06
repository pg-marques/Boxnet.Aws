using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
