using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.ProviderResponses
{
    public class BookingWebhookResponseService: IBookingWebhookResponseService
    {
        public BookingWebhookResponseService(
             IDataProviderFactory dataProviderFactory,
             IBookingRecordsManager bookingRecordsManager,
             IBookingService bookingService,
             IAgentContext agentContext)
        {
            _dataProviderFactory = dataProviderFactory;
            _bookingRecordsManager = bookingRecordsManager;
            _agentContext = agentContext;
            _bookingService = bookingService;
        }
        
        
        public async Task<Result> ProcessBookingData(Stream stream, DataProviders dataProvider, RequestMetadata requestMetadata)
        {
            if (!AsyncDataProviders.Contains(dataProvider))
                return Result.Fail($"{nameof(dataProvider)} '{dataProvider}' isn't asynchronous." +
                    $"Asynchronous data providers: {string.Join(", ", AsyncDataProviders)}");
            
            var (_, isGettingBookingDetailsFailure, bookingDetails, gettingBookingDetailsError) = await _dataProviderFactory.Get(dataProvider).ProcessAsyncResponse(stream, requestMetadata);
            if (isGettingBookingDetailsFailure)
                return Result.Fail(gettingBookingDetailsError.Detail);
            
            var (_, isGetBookingFailure, booking, getBookingError) = await _bookingRecordsManager.Get(bookingDetails.ReferenceCode);
            
            if (isGetBookingFailure)
                return Result.Fail(getBookingError);
            
            await _agentContext.SetAgentInfo(booking.AgentId);
            
            await _bookingService.ProcessResponse(bookingDetails, booking, requestMetadata);
            return Result.Ok();
        }

        private readonly IDataProviderFactory _dataProviderFactory;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IAgentContext _agentContext;
        private readonly IBookingService _bookingService;
        private static readonly List<DataProviders> AsyncDataProviders = new List<DataProviders>{DataProviders.Netstorming, DataProviders.Etg};
    }
}