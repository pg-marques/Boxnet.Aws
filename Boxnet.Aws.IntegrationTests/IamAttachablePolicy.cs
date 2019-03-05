namespace Boxnet.Aws.IntegrationTests
{
    public class IamAttachablePolicy
    {
        public IamAttachablePolicyId Id { get; private set; }
        public string Description { get; }
        public IIamPolicyDocument Document { get; }
        public string Path { get; }

        public IamAttachablePolicy(IamAttachablePolicyId id, string description, IIamPolicyDocument document, string path)
        {
            Id = id;
            Description = description;
            Document = document;
            Path = path;
        }

        public void SetArn(string arn)
        {
            Id.SetArn(arn);
        }
    }
}
