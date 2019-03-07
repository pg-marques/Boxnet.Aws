namespace Boxnet.Aws.Model.Aws.Iam.Roles
{
    public class IamRoleResourceId : ResourceId<IamRoleResourceId>
    {
        public IamRoleResourceId(string name) : base(name) { }

        public IamRoleResourceId(string name, string arn) : base(name, arn) { }
    }
}
