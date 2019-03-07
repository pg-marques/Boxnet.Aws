using System.Collections.Generic;

namespace Boxnet.Aws.Model.Aws
{
    public class ResourceId<T> : IResourceId
        where T : ResourceId<T>
    {
        private readonly IList<string> aliases = new List<string>();

        public string Arn { get; private set; }

        public string Name { get; }

        public IEnumerable<string> Aliases { get { return aliases; } }

        public ResourceId(string name) : this(name, null) { }

        public ResourceId(string name, string arn)
        {
            Name = name;
            Arn = arn;
        }

        public void AddAlias(string alias)
        {
            aliases.Add(alias);
        }

        public void AddAliases(IEnumerable<string> aliases)
        {
            foreach(var alias in aliases)
                AddAlias(alias);
        }

        public void SetArn(string arn)
        {
            Arn = arn;
        }

        //protected override bool EqualsOverrided(T other)
        //{
        //    throw new NotImplementedException();
        //}

        //protected override int GetHashCodeOverrided()
        //{
        //    throw new NotImplementedException();
        //}
    }
}
