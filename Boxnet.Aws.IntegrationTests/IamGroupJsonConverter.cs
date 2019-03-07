using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamGroupJsonConverter : JsonConverter
    {        
        private const string PathField = "path";
        private const string IdField = "id";
        private const string ResourceIdField = "resourceId";
        private const string AliasesField = "aliases";
        private const string GuidField = "value";
        private const string NameField = "name";
        private const string ArnField = "arn";     

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IamGroup);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var @object = new JObjectAdapter(JObject.Load(reader));

            return new IamGroup(
                ExtractIdFrom(@object), 
                ExtractResourceIdFrom(@object), 
                @object[PathField].AsStringOrEmpty());
        }

        private IamGroupId ExtractIdFrom(JObjectAdapter @object)
        {
            var token = @object[IdField];
            return new IamGroupId(new Guid(token[GuidField].AsStringOrEmpty()));
        }

        private IamGroupResourceId ExtractResourceIdFrom(JObjectAdapter @object)
        {
            var token = @object[ResourceIdField];

            var aliases = token[AliasesField].AsEnumerableStringOrEmpty();
            var id = new IamGroupResourceId(
                token[NameField].AsStringOrEmpty(),
                token[ArnField].AsStringOrEmpty());

            foreach (var alias in aliases)
                id.AddAlias(alias);

            return id;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
