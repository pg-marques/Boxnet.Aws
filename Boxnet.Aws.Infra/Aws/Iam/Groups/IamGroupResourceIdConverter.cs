using Boxnet.Aws.Model.Aws.Iam.Groups;

namespace Boxnet.Aws.Infra.Aws.Iam.Groups
{
    public class IamGroupResourceIdConverter : ResourceIdConverter<IamGroupResourceId>
    {
        protected override IamGroupResourceId CreateResourceId(string name, string arn)
        {
            return new IamGroupResourceId(name, arn);
        }
    }
}
