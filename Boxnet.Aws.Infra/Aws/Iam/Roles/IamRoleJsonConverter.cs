using Boxnet.Aws.Infra.Aws.Iam.Policies;
using Boxnet.Aws.Infra.Core.Json;
using Boxnet.Aws.Model.Aws.Iam.Policies;
using Boxnet.Aws.Model.Aws.Iam.Roles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Boxnet.Aws.Infra.Aws.Iam.Roles
{
    public class IamRoleJsonConverter : JsonConverter
    {
        private const string PathField = "path";
        private const string DescriptionField = "description";
        private const string MaxSessionDurationField = "maxSessionDuration";
        private const string DocumentField = "assumeRolePolicyDocument";
        private const string DocumentValueField = "value";
        private const string AttachedPoliciesIdsField = "attachedPoliciesIds";
        private const string InlinePoliciesField = "inlinePolicies";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IamRole);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var wrapper = new JObjectWrapper(JObject.Load(reader));

            var role = new IamRole(
                ExtractIdFrom(wrapper),
                ExtractResourceIdFrom(wrapper),
                wrapper[PathField].AsStringOrEmpty(),
                wrapper[DescriptionField].AsStringOrEmpty(),
                wrapper[MaxSessionDurationField].As<int>(),
                ExtractDocumentFrom(wrapper));

            role.AddInlinePolicies(wrapper[InlinePoliciesField].AsEnumerable(new IamInlinePolicyConverter()));
            role.AddAttachedPoliciesIds(wrapper[AttachedPoliciesIdsField].AsEnumerable(new IamAttachablePolicyResourceIdConverter()));

            return role;
        }

        private IamRoleResourceId ExtractResourceIdFrom(JObjectWrapper wrapper)
        {
            return new IamRoleResourceIdConverter().Convert(wrapper);
        }

        private IamRoleId ExtractIdFrom(JObjectWrapper wrapper)
        {            
            return new IamRoleIdConverter().Convert(wrapper);
        }

        private IIamPolicyDocument ExtractDocumentFrom(JObjectWrapper wrapper)
        {
            var tokenWrapper = wrapper[DocumentField];

            return new IamPolicyDocument(tokenWrapper[DocumentValueField].AsStringOrEmpty());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
