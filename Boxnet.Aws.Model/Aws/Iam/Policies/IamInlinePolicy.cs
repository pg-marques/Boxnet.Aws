namespace Boxnet.Aws.Model.Aws.Iam.Policies
{
    public class IamInlinePolicy
    {
        public IamInlinePolicyResourceId Id { get; }
        public IIamPolicyDocument Document { get; }

        public IamInlinePolicy(IamInlinePolicyResourceId id, IIamPolicyDocument document)
        {
            Id = id;
            Document = document;
        }
    }
}
