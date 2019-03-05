using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamAttachablePoliciesJsonFileRepository : IIamAttachablePoliciesRepository
    {
        private readonly string filePath;

        public IamAttachablePoliciesJsonFileRepository(string filePath)
        {
            this.filePath = filePath;
        }

        public async Task AddAsync(IamAttachablePolicy policy)
        {
            var collection = await LoadCollectionAsyn();

            collection.Add(policy);            

            await SaveAsync(AsBytes(collection));
        }

        private static byte[] AsBytes(List<IamAttachablePolicy> collection)
        {
            var serialized = JsonConvert.SerializeObject(collection);

            return Encoding.UTF8.GetBytes(serialized);
        }

        private async Task SaveAsync(byte[] bytes)
        {
            var info = new FileInfo(this.filePath);
            if (!info.Directory.Exists)
                Directory.CreateDirectory(info.Directory.FullName);

            await File.WriteAllBytesAsync(this.filePath, bytes);
        }

        private async Task<List<IamAttachablePolicy>> LoadCollectionAsyn()
        {
            var info = new FileInfo(this.filePath);
            var collection = new List<IamAttachablePolicy>();

            if (!info.Exists)
                return collection;

            var file = await File.ReadAllBytesAsync(this.filePath);
            if (file == null || file.Length == 0)
                return collection;

            try
            {
                var serializedCollection = Encoding.UTF8.GetString(file);
                
                return JsonConvert.DeserializeObject<IEnumerable<IamAttachablePolicy>>(serializedCollection).ToList();
            }
            catch(Exception ex)
            {
                return collection;
            }
        }

        public Task<IEnumerable<IamAttachablePolicy>> ByAsync()
        {
            throw new NotImplementedException();
        }

        public IamAttachablePolicy ByIdAsync(IamAttachablePolicyId id)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(IamAttachablePolicy policy)
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync(IamAttachablePolicy policy)
        {
            throw new NotImplementedException();
        }
    }
}
