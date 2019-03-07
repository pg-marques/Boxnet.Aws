using Newtonsoft.Json.Linq;

namespace Boxnet.Aws.Infra.Core.Json
{
    public interface IJTokenConverter<T>
    {
        T Convert(JToken token);
    }
}
