using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.ResponseProcessing;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.SupplierResponses
{
    public class WebhookResponseService
    {
        public WebhookResponseService(ISupplierConnectorManager supplierConnectorManager,
             IBookingResponseProcessor responseProcessor)
        {
            _supplierConnectorManager = supplierConnectorManager;
            _responseProcessor = responseProcessor;
        }
        
        
        public async Task<Result> ProcessBookingData(Stream stream, Suppliers supplier)
        {
            var (_, isGettingBookingDetailsFailure, bookingDetails, gettingBookingDetailsError) = await _supplierConnectorManager.Get(supplier).ProcessAsyncResponse(stream);
            if (isGettingBookingDetailsFailure)
                return Result.Failure(gettingBookingDetailsError.Detail);

            await _responseProcessor.ProcessResponse(bookingDetails, UserInfo.InternalServiceAccount, BookingChangeEvents.SupplierWebHook,
                BookingChangeInitiators.Supplier); 
            
            return Result.Success();
        }

        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IBookingResponseProcessor _responseProcessor;
    }
}