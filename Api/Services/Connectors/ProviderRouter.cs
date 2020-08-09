using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    // TODO: Consider removing this class
    public class ProviderRouter : IProviderRouter
    {
        public ProviderRouter(IDataProviderFactory dataProviderFactory)
        {
            _dataProviderFactory = dataProviderFactory;
        }


        public Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline?, ProblemDetails>> GetExactAvailability(DataProviders dataProvider,
            string availabilityId, Guid roomContractSetId, string languageCode)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetExactAvailability(availabilityId, roomContractSetId, languageCode);
        }


        public Task<Result<AccommodationDetails, ProblemDetails>> GetAccommodation(DataProviders dataProvider, string id, string languageCode)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetAccommodation(id, languageCode);
        }


        public Task<Result<BookingDetails, ProblemDetails>> Book(DataProviders dataProvider, BookingRequest request, string languageCode)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.Book(request, languageCode);
        }


        public Task<Result<VoidObject, ProblemDetails>> CancelBooking(DataProviders dataProvider, string referenceCode)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.CancelBooking(referenceCode);
        }


        public Task<Result<DeadlineDetails, ProblemDetails>> GetDeadline(DataProviders dataProvider, string availabilityId,
            Guid roomContractSetId, string languageCode)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetDeadline(availabilityId, roomContractSetId, languageCode);
        }
        
        
        public Task<Result<BookingDetails, ProblemDetails>> GetBookingDetails(DataProviders dataProvider, string referenceCode,
            string languageCode)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetBookingDetails(referenceCode, languageCode);
        }

        private readonly IDataProviderFactory _dataProviderFactory;
    }
}