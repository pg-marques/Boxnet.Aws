using Newtonsoft.Json.Linq;
using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class JObjectAdapter
    {
        public JObject Object { get; }

        public JTokenAdapter this[string attributeName]
        {
            get
            {
                JTokenAdapter adapter = null;
                if (Object.TryGetValue(attributeName, StringComparison.OrdinalIgnoreCase, out JToken token))
                    adapter = new JTokenAdapter(token);

                return adapter;
            }
        }

        public JObjectAdapter(JObject @object)
        {
            Object = @object;
        }
    }
}
