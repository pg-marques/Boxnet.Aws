using System.Web;

namespace Boxnet.Aws.Model.Aws.Iam.Policies
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
