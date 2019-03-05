namespace Boxnet.Aws.IntegrationTests
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
