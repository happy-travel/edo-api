using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Infrastructure;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IBookingsProcessingService
    {
        Task<Result<List<int>>> GetForCancellation(DateTime deadlineDate);

        Task<Result<ProcessResult>> Cancel(List<int> bookingIds, RequestMetadata requestMetadata);
    }
}