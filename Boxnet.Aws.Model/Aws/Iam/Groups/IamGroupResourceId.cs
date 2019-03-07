namespace Boxnet.Aws.Model.Aws.Iam.Groups
{
    public class IamGroupResourceId : ResourceId<IamGroupResourceId>
    {
        public IamGroupResourceId(string name) : base(name)
        {
        }

        public IamGroupResourceId(string name, string arn) : base(name, arn)
        {
        }
    }
}
