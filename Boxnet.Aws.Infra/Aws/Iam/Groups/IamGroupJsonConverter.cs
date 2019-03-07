using Boxnet.Aws.Infra.Aws.Iam.Policies;
using Boxnet.Aws.Infra.Core.Json;
using Boxnet.Aws.Model.Aws.Iam.Groups;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Boxnet.Aws.Infra.Aws.Iam.Groups
{
    public class IamGroupJsonConverter : JsonConverter
    {
        private const string PathField = "path";
        private const string AttachedPoliciesField = "attachedPolicies";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IamGroup);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var wrapper = new JObjectWrapper(JObject.Load(reader));
            var group = new IamGroup(ExtractIdFrom(wrapper), ExtractResourceIdFrom(wrapper), wrapper[PathField].AsStringOrEmpty());

            group.AddAttachedPoliciesIds(wrapper[AttachedPoliciesField].AsEnumerable(new IamAttachablePolicyResourceIdConverter()));

            return group;
        }

        private IamGroupResourceId ExtractResourceIdFrom(JObjectWrapper wrapper)
        {
            return new IamGroupResourceIdConverter().Convert(wrapper);
        }

        private IamGroupId ExtractIdFrom(JObjectWrapper wrapper)
        {
            return new IamGroupIdConverter().Convert(wrapper);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
