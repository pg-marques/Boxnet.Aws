namespace Boxnet.Aws.Model.Aws
{
    public interface IResourceIdFilter
    {
        bool IsSatisfiedBy(IResourceId resourceId);
    }
}
