namespace Boxnet.Aws.Model.Aws
{
    public interface IResource<TResourceId> where TResourceId : IResourceId
    {
        TResourceId ResourceId { get; }
    }
}
