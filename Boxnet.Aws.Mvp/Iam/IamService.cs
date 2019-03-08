using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;

namespace Boxnet.Aws.Mvp.Iam
{
    public class IamService : IDisposable
    {
        protected readonly AmazonIdentityManagementServiceClient sourceClient;
        protected readonly AmazonIdentityManagementServiceClient destinationClient;
        protected readonly Stack stack;
        protected readonly List<Tag> tags;


        public IamService(
            Stack stack,
            string sourceAccessKey,
            string sourceSecretKey,
            string sourceRegion,
            string destinationAccessKey,
            string destinationSecretKey,
            string destinationRegion)
        {
            this.stack = stack;
            sourceClient = new AmazonIdentityManagementServiceClient(new BasicAWSCredentials(sourceAccessKey, sourceSecretKey), RegionEndpoint.GetBySystemName(sourceRegion));
            destinationClient = new AmazonIdentityManagementServiceClient(new BasicAWSCredentials(destinationAccessKey, destinationSecretKey), RegionEndpoint.GetBySystemName(destinationRegion));
            tags = new List<Tag>()
            {
                new Tag()
                {
                    Key = "Project",
                    Value = stack.Name
                },
                new Tag()
                {
                    Key = "Environment",
                    Value = stack.Environment
                },
                new Tag()
                {
                    Key = "ProjectEnvironment",
                    Value = string.Format("{0}{1}", stack.Name, stack.Environment)
                }
            };
        }

        protected string NewNameFor(string name)
        {
            return string.Format("{0}{1}", Prefix(), name);
        }

        protected string Prefix()
        {
            return string.Format("{0}{1}_", stack.Name, stack.Environment);
        }

        protected string ExtracDocumentFrom(string document)
        {
            if (string.IsNullOrWhiteSpace(document))
                return string.Empty;

            return HttpUtility.UrlDecode(document);
        }

        #region IamPolicies

        public async Task CopyAllPoliciesAsync(string nameFilter)
        {
            var collectionData = await GetAllPoliciesFromSourceAsync(nameFilter);
            var collection = ConvertToPolicies(collectionData);

            await CreateAllPoliciesOnDestinationAsync(collection);

            stack.IamPolicies = collection;
        }

        private IEnumerable<IamPolicy> ConvertToPolicies(IEnumerable<Tuple<ManagedPolicy, string>> collection)
        {
            return collection.Select(item => new IamPolicy()
            {
                Description = item.Item1.Description,
                Document = ExtracDocumentFrom(item.Item2),
                Path = item.Item1.Path,
                PreviousArn = item.Item1.Arn,
                PreviousName = item.Item1.PolicyName,
                NewName = NewNameFor(item.Item1.PolicyName)
            });
        }

        private async Task CreateAllPoliciesOnDestinationAsync(IEnumerable<IamPolicy> collection)
        {
            foreach (var item in collection)
            {
                var request = new CreatePolicyRequest()
                {
                    Description = item.Description,
                    Path = item.Path,
                    PolicyDocument = item.Document,
                    PolicyName = item.NewName
                };

                var response = await destinationClient.CreatePolicyAsync(request);
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
                        document = defaultVersion.Document;
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

                collection.AddRange(response.Policies.Where(item => item.PolicyName.ToLower().Contains(nameFilter.ToLower())));

                marker = response.Marker;

            } while (marker != null);

            return collection;
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    sourceClient.Dispose();
                    destinationClient.Dispose();
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
