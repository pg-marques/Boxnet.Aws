namespace Boxnet.Aws.IntegrationTests
{
    public class IamUserResourceId : ResourceId<IamUserResourceId>
    {
        public IamUserResourceId(string name) : base(name)
        {
        }

        public IamUserResourceId(string name, string arn) : base(name, arn)
        {
        }
    }
}
