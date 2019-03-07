using Boxnet.Aws.Model.Aws.Iam.Users;

namespace Boxnet.Aws.Infra.Aws.Iam.Users
{
    public class IamUserResourceIdConverter : ResourceIdConverter<IamUserResourceId>
    {
        protected override IamUserResourceId CreateResourceId(string name, string arn)
        {
            return new IamUserResourceId(name, arn);
        }
    }
}
