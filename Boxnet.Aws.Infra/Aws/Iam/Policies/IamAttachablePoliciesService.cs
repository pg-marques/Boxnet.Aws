using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using Boxnet.Aws.Model.Aws;
using Boxnet.Aws.Model.Aws.Iam.Policies;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boxnet.Aws.Infra.Aws.Iam.Policies
{
    public class IamAttachablePoliciesService : IIamAttachablePoliciesService
    {
        private readonly AmazonIdentityManagementServiceClient client;

        public IamAttachablePoliciesService(string accessKeyId, string accessKey, string awsRegion)
        {
            client = new AmazonIdentityManagementServiceClient(new BasicAWSCredentials(accessKeyId, accessKey), RegionEndpoint.GetBySystemName(awsRegion));
        }

        public async Task<IEnumerable<IamAttachablePolicy>> ListByFilterAsync(IResourceIdFilter filter)
        {
            var details = await GetAccounstAuthorizationDetailsAsync(filter);

            return await GetPoliciesAsync(details.PoliciesArns);
        }

        private async Task<IEnumerable<IamAttachablePolicy>> GetPoliciesAsync(IEnumerable<string> policiesArns)
        {
            var policies = new List<IamAttachablePolicy>();

            foreach (var policyArn in policiesArns)
            {
                var response = await client.GetPolicyAsync(new GetPolicyRequest()
                {
                    PolicyArn = policyArn
                });

                var document = await GetDocumentOfPolicyAsync(policyArn);

                policies.Add(new IamAttachablePolicy(
                    new IamAttachablePolicyId(),
                    new IamAttachablePolicyResourceId(response.Policy.PolicyName, policyArn),
                    response.Policy.Description,
                    new IamPolicyUndecodedDocument(document),
                    response.Policy.Path));
            }

            return policies;
        }

        private async Task<string> GetDocumentOfPolicyAsync(string policyArn)
        {
            string marker = null;
            do
            {
                var response = await client.ListPolicyVersionsAsync(new ListPolicyVersionsRequest()
                {
                    PolicyArn = policyArn,
                    Marker = marker
                });

                marker = response.Marker;

                var defaultVersion = response.Versions.FirstOrDefault(version => version.IsDefaultVersion);

                if (defaultVersion != null)
                    return await GetPolicyVersionDocumentAsync(policyArn, defaultVersion);

            } while (marker != null);

            return null;
        }

        private async Task<string> GetPolicyVersionDocumentAsync(string policyArn, PolicyVersion defaultVersion)
        {
            var response = await client.GetPolicyVersionAsync(new GetPolicyVersionRequest()
            {
                PolicyArn = policyArn,
                VersionId = defaultVersion.VersionId
            });

            return response.PolicyVersion.Document;
        }

        private async Task<AccountsAuthorizationDetails> GetAccounstAuthorizationDetailsAsync(IResourceIdFilter filter)
        {
            var details = new AccountsAuthorizationDetails();

            string marker = null;
            do
            {
                var response = await client.GetAccountAuthorizationDetailsAsync(new GetAccountAuthorizationDetailsRequest()
                {
                    Marker = marker
                });

                details.Add(response);

                marker = response.Marker;

            } while (marker != null);

            return details.FilterBy(filter);
        }

        public async Task CreateAsync(IamAttachablePolicy policy)
        {
            var response = await client.CreatePolicyAsync(new CreatePolicyRequest()
            {
                Description = policy.Description,
                Path = policy.Path,
                PolicyDocument = policy.Document.Value,
                PolicyName = policy.ResourceId.Name
            });

            policy.SetArn(response.Policy.Arn);
        }

        public async Task DeleteAsync(IamAttachablePolicy policy)
        {
            await client.DeletePolicyAsync(new DeletePolicyRequest()
            {
                PolicyArn = policy.ResourceId.Arn
            });
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
