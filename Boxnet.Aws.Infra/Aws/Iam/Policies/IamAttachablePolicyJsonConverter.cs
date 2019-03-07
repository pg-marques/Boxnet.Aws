using Boxnet.Aws.Infra.Core.Json;
using Boxnet.Aws.Model.Aws.Iam.Policies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Boxnet.Aws.Infra.Aws.Iam.Policies
{
    public class IamAttachablePolicyJsonConverter : JsonConverter
    {
        private const string DescriptionField = "description";
        private const string PathField = "path";
        private const string DocumentField = "document";
        private const string DocumentValueField = "value";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IamAttachablePolicy);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var wrapper = new JObjectWrapper(JObject.Load(reader));

            return new IamAttachablePolicy(
                ExtractIdFrom(wrapper),
                ExtractResourceIdFrom(wrapper),
                wrapper[DescriptionField].AsStringOrEmpty(),
                ExtractDocumentFrom(wrapper),
                wrapper[PathField].AsStringOrEmpty());
        }

        private IamAttachablePolicyResourceId ExtractResourceIdFrom(JObjectWrapper wrapper)
        {
            return new IamAttachablePolicyResourceIdConverter().Convert(wrapper);
        }

        private IamAttachablePolicyId ExtractIdFrom(JObjectWrapper wrapper)
        {
            return new IamAttachablePolicyIdConverter().Convert(wrapper);
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
