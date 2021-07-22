using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Storage;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public interface IAvailabilityStorage
    {
        Task<List<CachedAccommodationAvailabilityResult>> Get(Expression<Func<CachedAccommodationAvailabilityResult, bool>> criteria);
        Task Save(List<CachedAccommodationAvailabilityResult> results);
    }
}