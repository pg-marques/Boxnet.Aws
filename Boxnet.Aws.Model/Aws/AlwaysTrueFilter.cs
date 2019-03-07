namespace Boxnet.Aws.Model.Aws
{
    public class AlwaysTrueFilter : IResourceIdFilter
    {
        public bool IsSatisfiedBy(IResourceId resourceId)
        {
            return true;
        }
    }
}
