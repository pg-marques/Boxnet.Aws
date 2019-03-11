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
