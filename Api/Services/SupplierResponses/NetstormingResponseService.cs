using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Infrastructure.SupplierConnectors;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.ResponseProcessing;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.SupplierResponses
{
    public class NetstormingResponseService
    {
        public NetstormingResponseService(IConnectorClient connectorClient,
            IBookingResponseProcessor responseProcessor,
            IOptions<SupplierOptions> supplierOptions,
            ILogger<NetstormingResponseService> logger)
        {
            _connectorClient = connectorClient;
            _supplierOptions = supplierOptions.Value;
            _responseProcessor = responseProcessor;
            _logger = logger;
        }


        public async Task<Result> ProcessBookingDetailsResponse(byte[] xmlRequestData)
        {
            var (_, isGetBookingDetailsFailure, bookingDetails , bookingDetailsError) = await GetBookingDetailsFromConnector(xmlRequestData);
            if (isGetBookingDetailsFailure)
            {
                _logger.LogUnableGetBookingDetailsFromNetstormingXml("Failed to get booking details from the Netstorming xml:" + 
                    Environment.NewLine + 
                    Encoding.UTF8.GetString(xmlRequestData));
                return Result.Failure(bookingDetailsError);
            }

            await _responseProcessor.ProcessResponse(bookingDetails, UserInfo.InternalServiceAccount, Common.Enums.BookingChangeEvents.SupplierWebHook,
                Common.Enums.BookingChangeInitiators.Supplier);

            return Result.Success();
        }
        

        private async Task<Result<Booking>> GetBookingDetailsFromConnector(byte[] xmlData)
        {
            var requestMessageFactory = new Func<HttpRequestMessage>(() => new HttpRequestMessage(HttpMethod.Post,
                new Uri($"{_supplierOptions.Netstorming}" + "bookings/response"))
            {
                Content = new ByteArrayContent(xmlData)
            });

            var (_, isFailure, bookingDetails, error) = await _connectorClient.Send<Booking>(requestMessageFactory);
            return isFailure 
                ? Result.Failure<Booking>(error.Detail) 
                : Result.Success(bookingDetails);
        }
        

        private readonly IConnectorClient _connectorClient;
        private readonly IBookingResponseProcessor _responseProcessor;
        private readonly SupplierOptions _supplierOptions;
        private readonly ILogger<NetstormingResponseService> _logger;
    }
}