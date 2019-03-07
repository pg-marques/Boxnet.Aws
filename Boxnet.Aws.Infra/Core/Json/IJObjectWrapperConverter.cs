namespace Boxnet.Aws.Infra.Core.Json
{
    public interface IJObjectWrapperConverter<T>
    {
        T Convert(JObjectWrapper wrapper);
    }
}
