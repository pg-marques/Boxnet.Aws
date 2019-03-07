using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Boxnet.Aws.Infra.Core.Json
{
    public class JTokenWrapper
    {
        public JToken Token { get; }

        public JTokenWrapper this[string attributeName]
        {
            get
            {
                if (Token == null || !Token.HasValues)
                    return new JTokenWrapper(null);

                var token = Token
                    .Where(item => ((JProperty)item).Name.ToLower() == attributeName.ToLower())
                    .Select(item => ((JProperty)item).Value).FirstOrDefault();

                return new JTokenWrapper(token);
            }
        }

        public JTokenWrapper(JToken token)
        {
            Token = token;
        }

        public string AsStringOrEmpty()
        {
            if (Token == null)
                return string.Empty;

            if (Token.Type == JTokenType.Object)
                return Token.ToString();

            return Token.Value<string>();
        }

        public T As<T>()
        {
            if (Token == null)
                return default(T);

            var serialized = (string) Token;

            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public IEnumerable<string> AsEnumerableStringOrEmpty()
        {
            if (Token == null)
                return Enumerable.Empty<string>();

            var children = Token.Values();
            if (children.Count() < 1)
                return Enumerable.Empty<string>();

            return children.Select(item => new JTokenWrapper(item).AsStringOrEmpty());
        }

        public IEnumerable<T> AsEnumerable<T>(IJTokenConverter<T> converter)
        {
            if (Token == null)
                return Enumerable.Empty<T>();

            return Token.Select(item => converter.Convert(item));
        }
    }
}
