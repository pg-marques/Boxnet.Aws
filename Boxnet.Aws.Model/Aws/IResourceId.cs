using System.Collections.Generic;

namespace Boxnet.Aws.Model.Aws
{
    public interface IResourceId
    {
        string Name { get; }
        string Arn { get; }
        IEnumerable<string> Aliases { get; }
        void AddAlias(string alias);
        void AddAliases(IEnumerable<string> aliases);
        void SetArn(string arn);
    }
}
