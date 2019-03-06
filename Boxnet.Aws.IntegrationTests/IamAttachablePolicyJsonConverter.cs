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
        private const string AliasesField = "aliases";
        private const string GuidField = "guid";
        private const string NameField = "name";
        private const string ArnField = "arn";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IamAttachablePolicy);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var @object = new JObjectAdapter(JObject.Load(reader));

            return new IamAttachablePolicy(
                ExtractIdFrom(@object),
                @object[DescriptionField].AsStringOrEmpty(),
                ExtractDocumentFrom(@object),
                @object[PathField].AsStringOrEmpty());
        }

        private IamAttachablePolicyId ExtractIdFrom(JObjectAdapter @object)
        {
            var token = @object[IdField];

            var aliases = token[AliasesField].AsEnumerableStringOrEmpty();
            var id = new IamAttachablePolicyId(
                new Guid(token[GuidField].AsStringOrEmpty()), 
                token[NameField].AsStringOrEmpty(), 
                token[ArnField].AsStringOrEmpty());            

            foreach (var alias in aliases)
                id.AddAlias(alias);

            return id;
        }

        private IIamPolicyDocument ExtractDocumentFrom(JObjectAdapter @object)
        {
            var token = @object["document"];

            return new IamPolicyDocument(token["value"].AsStringOrEmpty());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
