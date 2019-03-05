namespace Boxnet.Aws.IntegrationTests
{
    public class AlwaysTrueFilter : IResourceIdFilter
    {
        public bool IsSatisfiedBy(IResourceId resourceId)
        {
            return true;
        }
    }
}
