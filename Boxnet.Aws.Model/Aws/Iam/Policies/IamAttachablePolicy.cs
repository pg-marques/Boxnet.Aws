namespace Boxnet.Aws.Model.Aws.Iam.Policies
{
    public class IamAttachablePolicy : ResourceEntity<IamAttachablePolicyId, IamAttachablePolicyResourceId>
    {
        public string Description { get; private set; }
        public IIamPolicyDocument Document { get; }
        public string Path { get; }

        public IamAttachablePolicy(IamAttachablePolicyId id, IamAttachablePolicyResourceId resourceId, string description, IIamPolicyDocument document, string path)
            : base(id, resourceId)
        {
            Description = description;
            Document = document;
            Path = path;
        }

        public void ChangeDescription(string description)
        {
            Description = description;
        }
    }
}
