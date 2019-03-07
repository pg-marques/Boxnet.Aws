using Boxnet.Aws.Infra.Core.Json;
using Boxnet.Aws.Model.Aws.Iam.Policies;
using Newtonsoft.Json.Linq;

namespace Boxnet.Aws.Infra.Aws.Iam.Policies
{
    public class IamInlinePolicyConverter : IJTokenConverter<IamInlinePolicy>
    {
        private const string IdField = "id";
        private const string IdNameField = "name";
        private const string DocumentField = "document";

        public IamInlinePolicy Convert(JToken token)
        {
            var wrapper = new JTokenWrapper(token);
            var id = wrapper[IdField];

            return new IamInlinePolicy(
                new IamInlinePolicyResourceId(id[IdNameField].AsStringOrEmpty()),
                new IamPolicyDocument(wrapper[DocumentField].AsStringOrEmpty()));
        }
    }
}
