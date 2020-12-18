using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingRequestStorage : IBookingRequestStorage
    {
        public BookingRequestStorage(EdoContext edoContext)
        {
            _edoContext = edoContext;
        }
        
        
        public Task Set(string referenceCode, (AccommodationBookingRequest request, string availabilityId) requestInfo)
        {
            var request = new BookingRequest
            {
                AvailabilityId = requestInfo.availabilityId,
                ReferenceCode = referenceCode,
                RequestData = JsonConvert.SerializeObject(requestInfo.request)
            };
            _edoContext.BookingRequests.Add(request);
            return _edoContext.SaveChangesAsync();
        }


        public async Task<Result<(AccommodationBookingRequest request, string availabilityId)>> Get(string referenceCode)
        {
            var request = await _edoContext.BookingRequests
                .SingleOrDefaultAsync(b => b.ReferenceCode == referenceCode);

            if (request is null)
                return Result.Failure<(AccommodationBookingRequest, string)>($"Could not get booking request by reference code {referenceCode}");

            return (JsonConvert.DeserializeObject<AccommodationBookingRequest>(request.RequestData), request.AvailabilityId);
        }

        
        private readonly EdoContext _edoContext;
    }
}