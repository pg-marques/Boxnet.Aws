using Boxnet.Aws.Model.Aws.Iam.Policies;

namespace Boxnet.Aws.Infra.Aws.Iam.Policies
{
    public class IamAttachablePolicyResourceIdConverter : ResourceIdConverter<IamAttachablePolicyResourceId>
    {
        protected override IamAttachablePolicyResourceId CreateResourceId(string name, string arn)
        {
            return new IamAttachablePolicyResourceId(name, arn);
        }
    }
}
