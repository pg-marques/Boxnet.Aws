using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Boxnet.Aws.IntegrationTests
{
    public class JTokenAdapter
    {
        public JToken Token { get; }

        public JTokenAdapter this[string attributeName]
        {
            get
            {
                if (Token == null || !Token.HasValues)
                    return new JTokenAdapter(null);

                var token =  Token
                    .Where(item => ((JProperty)item).Name.ToLower() == attributeName.ToLower())
                    .Select(item => ((JProperty)item).Value).FirstOrDefault();

                return new JTokenAdapter(token);
            }
        }

        public JTokenAdapter(JToken token)
        {
            Token = token;
        }

        public string AsStringOrEmpty()
        {
            if (Token == null)
                return string.Empty;

            return (string)Token;
        }

        public T As<T>()
        {
            if (Token == null)
                return default(T);

            var serialized = AsStringOrEmpty();

            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public IEnumerable<string> AsEnumerableStringOrEmpty()
        {
            if (Token == null)
                return Enumerable.Empty<string>();

            var children = Token.Values();
            if (children.Count() < 1)
                return Enumerable.Empty<string>();

            return children.Select(item => (string)item);
        }
    }
}
