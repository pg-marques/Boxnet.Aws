namespace Boxnet.Aws.IntegrationTests
{
    public class IamInlinePolicy
    {
        public IamInlinePolicyId Id { get; }
        public IIamPolicyDocument Document { get; }

        public IamInlinePolicy(IamInlinePolicyId id, IIamPolicyDocument document)
        {
            Id = id;
            Document = document;
        }
    }
}
