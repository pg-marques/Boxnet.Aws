using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public abstract class JsonFileRepository<TEntity, TEntityId>
        where TEntityId : IEntityId
        where TEntity : Entity<TEntityId>
    {
        private readonly string filePath;
        protected virtual IEnumerable<JsonConverter> Converters { get; }

        public JsonFileRepository(string filePath)
        {
            this.filePath = filePath;
        }

        public async Task AddAsync(TEntity @object)
        {
            var collection = await LoadCollectionAsync();

            collection.Add(@object);

            await SaveAsync(collection);
        }

        private async Task SaveAsync(IEnumerable<TEntity> collection)
        {
            var serialized = JsonConvert.SerializeObject(collection);

            var bytes = Encoding.UTF8.GetBytes(serialized);

            await SaveAsync(bytes);
        }

        private async Task SaveAsync(byte[] bytes)
        {
            var info = new FileInfo(this.filePath);
            if (!info.Directory.Exists)
                Directory.CreateDirectory(info.Directory.FullName);

            await File.WriteAllBytesAsync(this.filePath, bytes);
        }

        private async Task<byte[]> ReadBytesFromFileAsync()
        {
            var info = new FileInfo(this.filePath);

            if (!info.Exists)
                return null;

            return await File.ReadAllBytesAsync(this.filePath);
        }

        private async Task<string> ReadStringFromFileAsync()
        {
            var bytes = await ReadBytesFromFileAsync();

            if (bytes == null || bytes.Length == 0)
                return null;

            return Encoding.UTF8.GetString(bytes);
        }

        private async Task<List<TEntity>> LoadCollectionAsync()
        {
            var collection = await AllAsync();
            return collection.ToList();
        }

        public async Task<IEnumerable<TEntity>> AllAsync()
        {
            var serializedCollection = await ReadStringFromFileAsync();

            if (serializedCollection == null)
                return Enumerable.Empty<TEntity>();

            if (MustUseConverters())
                return DeserializedCollectionUsingConverters(serializedCollection);

            return DeserializedCollection(serializedCollection);
        }

        private IEnumerable<TEntity> DeserializedCollection(string serializedCollection)
        {
            return JsonConvert.DeserializeObject<IEnumerable<TEntity>>(serializedCollection);
        }

        private IEnumerable<TEntity> DeserializedCollectionUsingConverters(string serializedCollection)
        {
            var settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            foreach (var converter in Converters)
                settings.Converters.Add(converter);

            return JsonConvert.DeserializeObject<IEnumerable<TEntity>>(serializedCollection, settings);
        }

        private bool MustUseConverters()
        {
            return Converters != null && Converters.Count() > 0;
        }

        public async Task<TEntity> ByAsync(TEntityId id)
        {
            var collection = await AllAsync();

            return collection.FirstOrDefault(item => item.Id.Equals(id));
        }

        public async Task DeleteAsync(TEntity entity)
        {
            var collection = await AllAsync();

            var filteredCollection = collection.Where(item => !item.Equals(entity));

            await SaveAsync(filteredCollection);
        }

        public async Task SaveAsync(TEntity entity)
        {
            var collection = await LoadCollectionAsync();

            var foundEntity = collection.FirstOrDefault(item => item.Equals(entity));

            if (foundEntity == null)
                throw new InvalidOperationException(string.Format("Entity not found. Entity: {0}", entity.ToString()));

            var index = collection.IndexOf(foundEntity);

            collection.RemoveAt(index);
            collection.Insert(index, entity);

            await SaveAsync(collection);
        }
    }
}
