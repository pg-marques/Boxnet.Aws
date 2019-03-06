using System;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamAttachablePolicy : Entity<IamAttachablePolicyId>, IResource<IamAttachablePolicyId>
    {
        public string Description { get; private set; }
        public IIamPolicyDocument Document { get; }
        public string Path { get; }

        public IamAttachablePolicy(IamAttachablePolicyId id, string description, IIamPolicyDocument document, string path) : base(id)
        {
            Description = description;
            Document = document;
            Path = path;
        }

        public void SetArn(string arn)
        {
            Id.SetArn(arn);
        }

        public void ChangeDescription(string description)
        {
            Description = description;
        }
    }
}
