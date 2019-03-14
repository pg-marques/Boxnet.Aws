using Amazon.IdentityManagement.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.Mvp.Iam
{
    public class IamPoliciesService : IamService
    {
        public IamPoliciesService(Stack stack, string sourceAccessKey, string sourceSecretKey, string sourceRegion, string destinationAccessKey, string destinationSecretKey, string destinationRegion) : base(stack, sourceAccessKey, sourceSecretKey, sourceRegion, destinationAccessKey, destinationSecretKey, destinationRegion)
        {
        }

        #region IamPolicies

        public async Task CopyAllPoliciesAsync(string nameFilter)
        {
            var collectionData = await GetAllPoliciesFromSourceAsync(nameFilter);
            var collection = ConvertToPolicies(collectionData);
            await UpdateWithExistingDataAsync(collection);
            await CreateAllPoliciesOnDestinationAsync(collection);

            stack.IamPolicies = collection;
        }

        private async Task UpdateWithExistingDataAsync(IEnumerable<IamPolicy> collection)
        {
            var existingCollectionData = await GetAllPoliciesFromDestinationAsync();
            var existingCollection = ConvertToPolicies(existingCollectionData);

            foreach(var policy in collection)
            {
                var existingPolicy = existingCollection.FirstOrDefault(it => it.PreviousName == policy.NewName);
                if (existingPolicy != null)
                    policy.NewArn = existingPolicy.PreviousArn;
            }      
        }

        private async Task<IEnumerable<Tuple<ManagedPolicy, string>>> GetAllPoliciesFromDestinationAsync()
        {
            var collection = await GetPoliciesFromDestinationAsync(Prefix());
            return await GetPoliciesWithDocumentsFromDestinationAsync(collection);
        }

        private async Task<IEnumerable<Tuple<ManagedPolicy, string>>> GetPoliciesWithDocumentsFromDestinationAsync(IEnumerable<ManagedPolicy> dataCollection)
        {
            var collection = new List<Tuple<ManagedPolicy, string>>();
            foreach (var item in dataCollection)
            {
                string marker = null;
                var document = string.Empty;
                do
                {
                    var request = new ListPolicyVersionsRequest()
                    {
                        Marker = marker,
                        PolicyArn = item.Arn
                    };
                    var response = await destinationClient.ListPolicyVersionsAsync(request);

                    var defaultVersion = response.Versions.FirstOrDefault(version => version.IsDefaultVersion);

                    if (defaultVersion != null)
                    {
                        var versionRequest = new GetPolicyVersionRequest()
                        {
                            PolicyArn = item.Arn,
                            VersionId = defaultVersion.VersionId
                        };

                        var versionResponse = await destinationClient.GetPolicyVersionAsync(versionRequest);

                        document = versionResponse.PolicyVersion.Document;
                        break;
                    }

                    marker = response.Marker;

                } while (marker != null);

                collection.Add(new Tuple<ManagedPolicy, string>(item, document));
            }

            return collection;
        }

        private async Task<List<ManagedPolicy>> GetPoliciesFromDestinationAsync(string nameFilter)
        {
            var collection = new List<ManagedPolicy>();

            string marker = null;
            do
            {
                var request = new ListPoliciesRequest()
                {
                    Marker = marker
                };

                var response = await destinationClient.ListPoliciesAsync(request);

                collection.AddRange(response.Policies.Where(item => item.PolicyName.ToLower().Contains(nameFilter.ToLower())));

                marker = response.Marker;

            } while (marker != null);

            return collection;
        }

        private List<IamPolicy> ConvertToPolicies(IEnumerable<Tuple<ManagedPolicy, string>> collection)
        {
            return collection.Select(item => new IamPolicy()
            {
                Description = item.Item1.Description,
                Document = ExtracDocumentFrom(item.Item2),
                Path = item.Item1.Path,
                PreviousArn = item.Item1.Arn,
                PreviousName = item.Item1.PolicyName,
                NewName = NewNameFor(item.Item1.PolicyName)
            }).ToList();
        }

        private async Task CreateAllPoliciesOnDestinationAsync(List<IamPolicy> collection)
        {
            foreach (var item in collection.Where(it => it.NewArn == null).ToList())
            {
                var request = new CreatePolicyRequest()
                {
                    Description = item.Description,
                    Path = item.Path,
                    PolicyDocument = item.Document,
                    PolicyName = item.NewName
                };

                var response = await destinationClient.CreatePolicyAsync(request);
                item.NewArn = response.Policy.Arn;
            }
        }

        private async Task<IEnumerable<Tuple<ManagedPolicy, string>>> GetAllPoliciesFromSourceAsync(string nameFilter)
        {
            var collection = await GetPoliciesAsync(nameFilter);
            return await GetPoliciesWithDocumentsAsync(collection);
        }

        private async Task<IEnumerable<Tuple<ManagedPolicy, string>>> GetPoliciesWithDocumentsAsync(IEnumerable<ManagedPolicy> dataCollection)
        {
            var collection = new List<Tuple<ManagedPolicy, string>>();
            foreach (var item in dataCollection)
            {
                string marker = null;
                var document = string.Empty;
                do
                {
                    var request = new ListPolicyVersionsRequest()
                    {
                        Marker = marker,
                        PolicyArn = item.Arn
                    };
                    var response = await sourceClient.ListPolicyVersionsAsync(request);

                    var defaultVersion = response.Versions.FirstOrDefault(version => version.IsDefaultVersion);

                    if (defaultVersion != null)
                    {
                        var versionRequest = new GetPolicyVersionRequest()
                        {
                            PolicyArn = item.Arn,
                            VersionId = defaultVersion.VersionId
                        };

                        var versionResponse = await sourceClient.GetPolicyVersionAsync(versionRequest);

                        document = versionResponse.PolicyVersion.Document;
                        break;
                    }

                    marker = response.Marker;

                } while (marker != null);

                collection.Add(new Tuple<ManagedPolicy, string>(item, document));
            }

            return collection;
        }

        private async Task<List<ManagedPolicy>> GetPoliciesAsync(string nameFilter)
        {
            var collection = new List<ManagedPolicy>();

            string marker = null;
            do
            {
                var request = new ListPoliciesRequest()
                {
                    Marker = marker
                };

                var response = await sourceClient.ListPoliciesAsync(request);

                collection.AddRange(response.Policies.Where(item => item.PolicyName.ToLower().Contains(nameFilter.ToLower()) && !item.PolicyName.ToLower().StartsWith(stack.Name.ToLower())));

                marker = response.Marker;

            } while (marker != null);

            return collection;
        }

        #endregion
    }
}
