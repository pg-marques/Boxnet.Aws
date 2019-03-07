namespace Boxnet.Aws.Model.Aws.Iam.Policies
{
    public class IamPolicyDocument : IIamPolicyDocument
    {
        public string Value { get; }

        public IamPolicyDocument(string document)
        {
            Value = document;
        }
    }
}
