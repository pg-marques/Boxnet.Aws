using Boxnet.Aws.Model.Aws.Iam.Groups;
using System.Collections.Generic;

namespace Boxnet.Aws.Model.Aws.Iam.Users
{
    public class IamUser : ResourceEntity<IamUserId, IamUserResourceId>
    {
        private readonly IList<IamGroupResourceId> groupIds = new List<IamGroupResourceId>();
        public string Path { get; }
        public IEnumerable<IamGroupResourceId> GroupsIds { get { return groupIds; } }

        public IamUser(IamUserId id, IamUserResourceId resourceId, string path) : base(id, resourceId)
        {
            Path = path;
        }

        public void AddGroupId(IamGroupResourceId groupId)
        {
            groupIds.Add(groupId);
        }

        public void AddGroupsIds(IEnumerable<IamGroupResourceId> groupsIds)
        {
            foreach(var groupId in groupsIds)
                AddGroupId(groupId);
        }
    }
}
