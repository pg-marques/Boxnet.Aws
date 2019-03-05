using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamRoleId : IResourceId
    {
        private readonly IList<string> aliases = new List<string>();

        public string Arn { get; private set; }

        public string Name { get; }

        public IEnumerable<string> Aliases { get { return aliases; } }

        public IamRoleId(string name) : this(null, name) { }

        public IamRoleId(string arn, string name)
        {
            Arn = arn;
            Name = name;
        }

        public void AddAlias(string alias)
        {
            aliases.Add(alias);
        }

        public void SetArn(string arn)
        {
            Arn = arn;
        }
    }
}
