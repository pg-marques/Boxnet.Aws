using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamRoleJsonConverter : JsonConverter
    {
        private const string PathField = "path";
        private const string DescriptionField = "description";
        private const string IdField = "id";
        private const string AliasesField = "aliases";
        private const string GuidField = "guid";
        private const string NameField = "name";
        private const string ArnField = "arn";
        private const string MaxSessionDurationField = "maxSessionDuration";
        private const string DocumentField = "assumeRolePolicyDocument";
        private const string ValueField = "value";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IamRole);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var @object = new JObjectAdapter(JObject.Load(reader));

            return new IamRole(
                ExtractIdFrom(@object),
                @object[PathField].AsStringOrEmpty(),
                @object[DescriptionField].AsStringOrEmpty(),
                @object[MaxSessionDurationField].As<int>(),
                ExtractDocumentFrom(@object));
        }

        private IamRoleId ExtractIdFrom(JObjectAdapter @object)
        {
            var token = @object[IdField];

            var aliases = token[AliasesField].AsEnumerableStringOrEmpty();
            var id = new IamRoleId(
                new Guid(token[GuidField].AsStringOrEmpty()),
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
