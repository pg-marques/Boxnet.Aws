using System.Web;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamPolicyUndecodedDocument : IIamPolicyDocument
    {
        public string Value { get; }

        public IamPolicyUndecodedDocument(string document)
        {
            Value = HttpUtility.UrlDecode(document);
        }
    }
}
