using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamUser : Entity<IamUserId>, IResource<IamUserId>
    {
        private readonly IList<IamGroupId> groupIds = new List<IamGroupId>();
        public string Path { get; }
        public IEnumerable<IamGroupId> GroupsIds { get { return groupIds; } }

        public IamUser(IamUserId id, string path):base(id)
        {
            Path = path;
        }

        public void AddGroupId(IamGroupId groupId)
        {
            groupIds.Add(groupId);
        }

        public void SetArn(string arn)
        {
            Id.SetArn(arn);
        }
    }
}
