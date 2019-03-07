namespace Boxnet.Aws.IntegrationTests
{
    public class IamAttachablePolicyResourceId : ResourceId<IamAttachablePolicyResourceId>
    {
        public IamAttachablePolicyResourceId(string name) : base(name)
        {
        }

        public IamAttachablePolicyResourceId(string name, string arn) : base(name, arn)
        {
        }
    }
}
