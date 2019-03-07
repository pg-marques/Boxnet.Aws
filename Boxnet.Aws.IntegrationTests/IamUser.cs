using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamUser : ResourceEntity<IamUserId,IamUserResourceId>
    {
        private readonly IList<IamGroupResourceId> groupIds = new List<IamGroupResourceId>();
        public string Path { get; }
        public IEnumerable<IamGroupResourceId> GroupsIds { get { return groupIds; } }

        public IamUser(IamUserId id, IamUserResourceId resourceId, string path):base(id, resourceId)
        {
            Path = path;
        }

        public void AddGroupId(IamGroupResourceId groupId)
        {
            groupIds.Add(groupId);
        }
    }
}
