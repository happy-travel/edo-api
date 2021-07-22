using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Storage;
using HappyTravel.SuppliersCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public class AvailabilityStorage : IAvailabilityStorage
    {
        public AvailabilityStorage(IOptions<AvailabilityStorageSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);

            _results = database.GetCollection<CachedAccommodationAvailabilityResult>(settings.Value.CollectionName);
            
            var searchIndexDefinition = Builders<CachedAccommodationAvailabilityResult>.IndexKeys.Combine(
                Builders<CachedAccommodationAvailabilityResult>.IndexKeys.Ascending(f => f.SearchId),
                Builders<CachedAccommodationAvailabilityResult>.IndexKeys.Ascending(f => f.Supplier));

            var ttlIndexDefinition = Builders<CachedAccommodationAvailabilityResult>.IndexKeys.Ascending(f => f.Created);
            var ttlIndexOptions = new CreateIndexOptions {ExpireAfter = TimeSpan.FromMinutes(45)};
            
            _results.Indexes.CreateMany(new []
            {
                new CreateIndexModel<CachedAccommodationAvailabilityResult>(searchIndexDefinition),
                new CreateIndexModel<CachedAccommodationAvailabilityResult>(ttlIndexDefinition, ttlIndexOptions)
            });
        }
        
        
        public Task Save(List<CachedAccommodationAvailabilityResult> results) 
            => _results.InsertManyAsync(results);


        public Task<List<CachedAccommodationAvailabilityResult>> Get(Expression<Func<CachedAccommodationAvailabilityResult, bool>> criteria) 
            => _results.Find(criteria).ToListAsync();


        private readonly IMongoCollection<CachedAccommodationAvailabilityResult> _results;
    }
}