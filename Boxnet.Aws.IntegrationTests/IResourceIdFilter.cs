namespace Boxnet.Aws.IntegrationTests
{
    public interface IResourceIdFilter
    {
        bool IsSatisfiedBy(IResourceId resourceId);
    }
}
