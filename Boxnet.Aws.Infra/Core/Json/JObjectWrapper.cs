using Newtonsoft.Json.Linq;
using System;

namespace Boxnet.Aws.Infra.Core.Json
{
    public class JObjectWrapper
    {
        public JObject Object { get; }

        public JTokenWrapper this[string attributeName]
        {
            get
            {
                JTokenWrapper adapter = null;
                if (Object.TryGetValue(attributeName, StringComparison.OrdinalIgnoreCase, out JToken token))
                    adapter = new JTokenWrapper(token);

                return adapter;
            }
        }

        public JObjectWrapper(JObject @object)
        {
            Object = @object;
        }
    }
}
