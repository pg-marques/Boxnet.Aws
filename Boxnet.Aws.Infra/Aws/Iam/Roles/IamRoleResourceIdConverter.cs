using Boxnet.Aws.Model.Aws.Iam.Roles;

namespace Boxnet.Aws.Infra.Aws.Iam.Roles
{
    public class IamRoleResourceIdConverter : ResourceIdConverter<IamRoleResourceId>
    {
        protected override IamRoleResourceId CreateResourceId(string name, string arn)
        {
            return new IamRoleResourceId(name, arn);
        }
    }
}
