using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public interface IResourceId : IEntityId
    {
        string Name { get; }
        string Arn { get; }
        IEnumerable<string> Aliases { get; }
        void AddAlias(string alias);
        void SetArn(string arn);
    }
}
