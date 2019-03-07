namespace Boxnet.Aws.IntegrationTests
{
    public abstract class ResourceEntity<TEntityId, TResourceId> : Entity<TEntityId>, IResource<TResourceId>
        where TEntityId : IEntityId
        where TResourceId : IResourceId
    {
        public TResourceId ResourceId { get; }

        public ResourceEntity(TEntityId id, TResourceId resourceId) : base(id)
        {
            ResourceId = resourceId;
        }

        public void SetArn(string arn)
        {
            ResourceId.SetArn(arn);
        }
    }
}
