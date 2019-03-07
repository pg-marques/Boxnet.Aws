using Boxnet.Aws.Infra.Core.Json;
using Boxnet.Aws.Model.Aws;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Boxnet.Aws.Infra.Aws
{
    public abstract class ResourceIdConverter<TResourceId> : IJTokenConverter<TResourceId>, IJObjectWrapperConverter<TResourceId>
        where TResourceId : IResourceId
    {
        private const string AliasesField = "aliases";
        private const string NameField = "name";
        private const string ArnField = "arn";
        private const string ResourceIdField = "resourceId";

        public TResourceId Convert(JToken token)
        {
            return Convert(new JTokenWrapper(token));
        }

        public virtual TResourceId Convert(JObjectWrapper wrapper)
        {
            return Convert(ExtractResourceIdWrapperFrom(wrapper));
        }

        private string ExtractNameFrom(JTokenWrapper wrapper)
        {
            return wrapper[NameField].AsStringOrEmpty();
        }

        private TResourceId Convert(JTokenWrapper wrapper)
        {
            var id = CreateResourceId(ExtractNameFrom(wrapper), ExtractArnFrom(wrapper));
            id.AddAliases(ExtractAliasesFrom(wrapper));

            return id;
        }

        private JTokenWrapper ExtractResourceIdWrapperFrom(JObjectWrapper wrapper)
        {
            return wrapper[ResourceIdField];
        }

        private string ExtractArnFrom(JTokenWrapper wrapper)
        {
            return wrapper[ArnField].AsStringOrEmpty();
        }

        private IEnumerable<string> ExtractAliasesFrom(JTokenWrapper wrapper)
        {
            return wrapper[AliasesField].AsEnumerableStringOrEmpty();
        }

        protected abstract TResourceId CreateResourceId(string name, string arn);
    }
}
