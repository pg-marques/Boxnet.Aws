using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamAttachablePolicyJsonConverter : JsonConverter
    {
        private const string DescriptionField = "description";
        private const string PathField = "path";
        private const string IdField = "id";
        private const string ResourceIdField = "resourceId";
        private const string AliasesField = "aliases";
        private const string GuidField = "value";
        private const string NameField = "name";
        private const string ArnField = "arn";
        private const string DocumentField = "document";
        private const string ValueField = "value";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IamAttachablePolicy);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var @object = new JObjectAdapter(JObject.Load(reader));

            return new IamAttachablePolicy(
                ExtractIdFrom(@object),
                ExtractResourceIdFrom(@object),
                @object[DescriptionField].AsStringOrEmpty(),
                ExtractDocumentFrom(@object),
                @object[PathField].AsStringOrEmpty());
        }

        private IamAttachablePolicyId ExtractIdFrom(JObjectAdapter @object)
        {
            var token = @object[IdField];
            return new IamAttachablePolicyId(new Guid(token[GuidField].AsStringOrEmpty()));
        }

        private IamAttachablePolicyResourceId ExtractResourceIdFrom(JObjectAdapter @object)
        {
            var token = @object[ResourceIdField];

            var aliases = token[AliasesField].AsEnumerableStringOrEmpty();
            var id = new IamAttachablePolicyResourceId(
                token[NameField].AsStringOrEmpty(), 
                token[ArnField].AsStringOrEmpty());            

            foreach (var alias in aliases)
                id.AddAlias(alias);

            return id;
        }

        private IIamPolicyDocument ExtractDocumentFrom(JObjectAdapter @object)
        {
            var token = @object[DocumentField];

            return new IamPolicyDocument(token[ValueField].AsStringOrEmpty());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
