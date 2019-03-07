using Boxnet.Aws.Infra.Aws.Iam.Groups;
using Boxnet.Aws.Infra.Core.Json;
using Boxnet.Aws.Model.Aws.Iam.Users;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Boxnet.Aws.Infra.Aws.Iam.Users
{
    public class IamUserJsonConverter : JsonConverter
    {
        private const string PathField = "path";
        private const string GroupsIdsField = "groupsIds";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IamUser);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var wrapper = new JObjectWrapper(JObject.Load(reader));

            var user = new IamUser(ExtractIdFrom(wrapper), ExtractResourceIdFrom(wrapper), wrapper[PathField].AsStringOrEmpty());

            user.AddGroupsIds(wrapper[GroupsIdsField].AsEnumerable(new IamGroupResourceIdConverter()));

            return user;
        }

        private IamUserResourceId ExtractResourceIdFrom(JObjectWrapper wrapper)
        {
            return new IamUserResourceIdConverter().Convert(wrapper);
        }

        private IamUserId ExtractIdFrom(JObjectWrapper wrapper)
        {
            return new IamUserIdConverter().Convert(wrapper);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
