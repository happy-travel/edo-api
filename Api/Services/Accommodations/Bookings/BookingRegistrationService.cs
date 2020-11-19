using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.Mailing;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.Edo.Data;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingRegistrationService : IBookingRegistrationService
    {
        public BookingRegistrationService(IAccommodationBookingSettingsService accommodationBookingSettingsService,
            IBookingRecordsManager bookingRecordsManager,
            IBookingDocumentsService documentsService,
            IPaymentNotificationService notificationService,
            IBookingMailingService bookingMailingService,
            IDateTimeProvider dateTimeProvider,
            IAccountPaymentService accountPaymentService,
            ISupplierConnectorManager supplierConnectorManager,
            IBookingPaymentService paymentService,
            IBookingEvaluationStorage bookingEvaluationStorage,
            EdoContext context,
            IBookingResponseProcessor bookingResponseProcessor,
            ILogger<BookingRegistrationService> logger)
        {
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
            _bookingRecordsManager = bookingRecordsManager;
            _documentsService = documentsService;
            _notificationService = notificationService;
            _bookingMailingService = bookingMailingService;
            _dateTimeProvider = dateTimeProvider;
            _accountPaymentService = accountPaymentService;
            _supplierConnectorManager = supplierConnectorManager;
            _paymentService = paymentService;
            _bookingEvaluationStorage = bookingEvaluationStorage;
            _context = context;
            _bookingResponseProcessor = bookingResponseProcessor;
            _logger = logger;
        }
        
        
        public async Task<Result<string, ProblemDetails>> Register(AccommodationBookingRequest bookingRequest, AgentContext agentContext, string languageCode)
        {
            string availabilityId = default;
            var settings = await _accommodationBookingSettingsService.Get(agentContext);

            return await GetCachedAvailability(bookingRequest, agentContext)
                .Ensure(AreAprSettingsSuitable, ProblemDetailsBuilder.Build("You can't book the restricted contract without explicit approval from a Happytravel.com officer."))
                .Ensure(AreDeadlineSettingsSuitable, ProblemDetailsBuilder.Build("You can't book the contract within deadline without explicit approval from a Happytravel.com officer."))
                .Tap(FillAvailabilityId)
                .Map(ExtractBookingAvailabilityInfo)
                .Map(Register)
                .Finally(WriteLog);


            bool AreAprSettingsSuitable(
                (Suppliers, DataWithMarkup<RoomContractSetAvailability>) bookingData)
                => BookingRegistrationService.AreAprSettingsSuitable(bookingRequest, bookingData, settings);


            bool AreDeadlineSettingsSuitable(
                (Suppliers, DataWithMarkup<RoomContractSetAvailability>) bookingData)
                => this.AreDeadlineSettingsSuitable(bookingRequest, bookingData, settings);


            void FillAvailabilityId((Suppliers, DataWithMarkup<RoomContractSetAvailability> Result) responseWithMarkup)
                => availabilityId = responseWithMarkup.Result.Data.AvailabilityId;


            async Task<string> Register(BookingAvailabilityInfo bookingAvailability)
            {
                var bookingRequestWithAvailabilityId = new AccommodationBookingRequest(bookingRequest, availabilityId);
                return await _bookingRecordsManager.Register(bookingRequestWithAvailabilityId, bookingAvailability, agentContext, languageCode);
            }


            Result<string, ProblemDetails> WriteLog(Result<string, ProblemDetails> result)
                => LoggerUtils.WriteLogByResult(result,
                    () => _logger.LogBookingRegistrationSuccess($"Successfully registered a booking with reference code: '{result.Value}'"),
                    () => _logger.LogBookingRegistrationFailure($"Failed to register a booking. AvailabilityId: '{availabilityId}'. " +
                        $"Itinerary number: {bookingRequest.ItineraryNumber}. Passenger name: {bookingRequest.MainPassengerName}. Error: {result.Error.Detail}"));
        }
        
        public async Task<Result<AccommodationBookingInfo, ProblemDetails>> Finalize(string referenceCode, AgentContext agentContext, string languageCode)
        {
            var (_, isGetBookingFailure, booking, getBookingError) = await GetAgentsBooking()
                .Ensure(b => agentContext.IsUsingAgency(b.AgencyId), ProblemDetailsBuilder.Build("The booking does not belong to your current agency"))
                .Bind(CheckBookingIsPaid)
                .OnFailure(WriteLogFailure);

            if (isGetBookingFailure)
                return Result.Failure<AccommodationBookingInfo, ProblemDetails>(getBookingError);

            return await BookOnProvider(booking, referenceCode, languageCode, false)
                .Tap(CreditCardPaymentNotify)
                .Bind(GenerateInvoice)
                .Bind(GetAccommodationBookingInfo)
                .Tap(NotifyBookingFinalized)
                .Tap(SendInvoice)
                .Finally(WriteLog);


            Task<Result<Data.Booking.Booking, ProblemDetails>> GetAgentsBooking()
                => _bookingRecordsManager.GetAgentsBooking(referenceCode, agentContext).ToResultWithProblemDetails();


            Result<Data.Booking.Booking, ProblemDetails> CheckBookingIsPaid(Data.Booking.Booking bookingFromPipe)
            {
                if (bookingFromPipe.PaymentStatus == BookingPaymentStatuses.NotPaid)
                {
                    _logger.LogBookingFinalizationPaymentFailure($"The booking with reference code: '{referenceCode}' hasn't been paid");
                    return ProblemDetailsBuilder.Fail<Data.Booking.Booking>("The booking hasn't been paid");
                }

                return bookingFromPipe;
            }


            async Task<Result<Booking, ProblemDetails>> CreditCardPaymentNotify(Booking bookingFromPipe)
            {
                await _bookingMailingService.NotifyCreditCardPaymentConfirmation(bookingFromPipe.ReferenceCode);
                return Result.Success<Booking, ProblemDetails>(bookingFromPipe);
            }


            Task NotifyBookingFinalized(AccommodationBookingInfo bookingInfo) => _bookingMailingService.NotifyBookingFinalized(bookingInfo);


            Task<Result<EdoContracts.Accommodations.Booking, ProblemDetails>> GenerateInvoice(EdoContracts.Accommodations.Booking details) => this.GenerateInvoice(details, referenceCode, agentContext);


            Task<Result<AccommodationBookingInfo, ProblemDetails>> GetAccommodationBookingInfo(Booking details)
                => _bookingRecordsManager.GetAccommodationBookingInfo(details.ReferenceCode, languageCode)
                    .ToResultWithProblemDetails();
            
            
            Task SendInvoice(AccommodationBookingInfo bookingInfo) => _bookingMailingService.SendInvoice(bookingInfo.BookingId, agentContext.Email, agentContext, languageCode);


            void WriteLogFailure(ProblemDetails problemDetails)
                => _logger.LogBookingByAccountFailure($"Failed to finalize a booking with reference code: '{referenceCode}'. Error: {problemDetails.Detail}");


            Result<T, ProblemDetails> WriteLog<T>(Result<T, ProblemDetails> result)
                => LoggerUtils.WriteLogByResult(result,
                    () => _logger.LogBookingFinalizationSuccess($"Successfully finalized a booking with reference code: '{booking.ReferenceCode}'"),
                    () => _logger.LogBookingFinalizationFailure(
                        $"Failed to finalize a booking with reference code: '{booking.ReferenceCode}'. Error: {result.Error.Detail}"));
        }


        public async Task<Result<AccommodationBookingInfo, ProblemDetails>> BookByAccount(AccommodationBookingRequest bookingRequest,
            AgentContext agentContext, string languageCode, string clientIp)
        {
            string availabilityId = default;
            DateTime? availabilityDeadline = default;
            string referenceCode = default;
            var wasPaymentMade = false;
            var settings = await _accommodationBookingSettingsService.Get(agentContext);

            // TODO Remove lots of code duplication in account and card purchase booking
            var (_, isRegisterFailure, booking, registerError) = await GetCachedAvailability(bookingRequest, agentContext)
                .Ensure(AreAprSettingsSuitable, ProblemDetailsBuilder.Build("You can't book the restricted contract without explicit approval from a Happytravel.com officer."))
                .Ensure(AreDeadlineSettingsSuitable, ProblemDetailsBuilder.Build("You can't book the contract within deadline without explicit approval from a Happytravel.com officer."))
                .Tap(FillAvailabilityLocalVariables)
                .Map(ExtractBookingAvailabilityInfo)
                .BindWithTransaction(_context, info => Result.Success<BookingAvailabilityInfo, ProblemDetails>(info)
                    .Map(RegisterBooking)
                    .Bind(GetBooking)
                    .Bind(PayUsingAccountIfDeadlinePassed))
                .OnFailure(WriteLogFailure);

            if (isRegisterFailure)
                return Result.Failure<AccommodationBookingInfo, ProblemDetails>(registerError);

            return await BookOnProvider(booking, booking.ReferenceCode, languageCode)
                .Tap(ProcessResponse)
                .OnFailure(VoidMoneyAndCancelBooking)
                .Bind(GenerateInvoice)
                .Bind(SendReceiptIfPaymentMade)
                .Bind(GetAccommodationBookingInfo)
                .Tap(NotifyBookingFinalized)
                .Tap(SendInvoice)
                .Finally(WriteLog);


            void FillAvailabilityLocalVariables((Suppliers, DataWithMarkup<RoomContractSetAvailability> Result) responseWithMarkup)
            {
                availabilityId = responseWithMarkup.Result.Data.AvailabilityId;
                availabilityDeadline = responseWithMarkup.Result.Data.RoomContractSet.Deadline.Date;
            }
            
            
            bool AreAprSettingsSuitable(
                (Suppliers, DataWithMarkup<RoomContractSetAvailability>) bookingData)
                => BookingRegistrationService.AreAprSettingsSuitable(bookingRequest, bookingData, settings);


            bool AreDeadlineSettingsSuitable(
                (Suppliers, DataWithMarkup<RoomContractSetAvailability>) bookingData)
                => this.AreDeadlineSettingsSuitable(bookingRequest, bookingData, settings);


            async Task<string> RegisterBooking(BookingAvailabilityInfo bookingAvailability)
            {
                var bookingRequestWithAvailabilityId = new AccommodationBookingRequest(bookingRequest, availabilityId);
                var registeredReferenceCode =
                    await _bookingRecordsManager.Register(bookingRequestWithAvailabilityId, bookingAvailability, agentContext, languageCode);

                referenceCode = registeredReferenceCode;
                return registeredReferenceCode;
            }


            async Task<Result<Data.Booking.Booking, ProblemDetails>> GetBooking(string referenceCode)
                => await _bookingRecordsManager.Get(referenceCode).ToResultWithProblemDetails();


            async Task<Result<Data.Booking.Booking, ProblemDetails>> PayUsingAccountIfDeadlinePassed(Data.Booking.Booking bookingInPipeline)
            {
                if (!availabilityDeadline.HasValue || availabilityDeadline > _dateTimeProvider.UtcNow())
                    return bookingInPipeline;

                var (_, isPaymentFailure, _, paymentError) = await _accountPaymentService.Charge(bookingInPipeline, agentContext, clientIp);
                if (isPaymentFailure)
                    return ProblemDetailsBuilder.Fail<Data.Booking.Booking>(paymentError);

                wasPaymentMade = true;
                return bookingInPipeline;
            }


            Task ProcessResponse(EdoContracts.Accommodations.Booking bookingResponse) => _bookingResponseProcessor.ProcessResponse(bookingResponse, booking);

            Task VoidMoneyAndCancelBooking(ProblemDetails problemDetails) => this.VoidMoneyAndCancelBooking(booking, agentContext);

            Task<Result<EdoContracts.Accommodations.Booking, ProblemDetails>> GenerateInvoice(EdoContracts.Accommodations.Booking details) => this.GenerateInvoice(details, booking.ReferenceCode, agentContext);


            async Task<Result<EdoContracts.Accommodations.Booking, ProblemDetails>> SendReceiptIfPaymentMade(EdoContracts.Accommodations.Booking details)
                => wasPaymentMade
                    ? await SendReceipt(details, booking, agentContext)
                    : details;


            Task<Result<AccommodationBookingInfo, ProblemDetails>> GetAccommodationBookingInfo(EdoContracts.Accommodations.Booking details)
                => _bookingRecordsManager.GetAccommodationBookingInfo(details.ReferenceCode, languageCode)
                    .ToResultWithProblemDetails();
            

            Task NotifyBookingFinalized(AccommodationBookingInfo bookingInfo) => _bookingMailingService.NotifyBookingFinalized(bookingInfo);

            
            Task SendInvoice(AccommodationBookingInfo bookingInfo) => _bookingMailingService.SendInvoice(bookingInfo.BookingId, agentContext.Email, agentContext, languageCode);
            
            
            void WriteLogFailure(ProblemDetails problemDetails)
                => _logger.LogBookingByAccountFailure($"Failed to book using account. Reference code: '{referenceCode}'. Error: {problemDetails.Detail}");


            Result<T, ProblemDetails> WriteLog<T>(Result<T, ProblemDetails> result)
                => LoggerUtils.WriteLogByResult(result,
                    () => _logger.LogBookingFinalizationSuccess($"Successfully booked using account. Reference code: '{referenceCode}'"),
                    () => _logger.LogBookingFinalizationFailure(
                        $"Failed to book using account. Reference code: '{referenceCode}'. Error: {result.Error.Detail}"));
        }
        
        
        public async Task<Result<EdoContracts.Accommodations.Booking, ProblemDetails>> SendReceipt(EdoContracts.Accommodations.Booking details, Data.Booking.Booking booking, AgentContext agentContext)
        {
            var (_, isReceiptFailure, receiptInfo, receiptError) = await _documentsService.GenerateReceipt(booking.Id, agentContext.AgentId);
            if (isReceiptFailure)
                return ProblemDetailsBuilder.Fail<EdoContracts.Accommodations.Booking>(receiptError);

            await _notificationService.SendReceiptToCustomer(receiptInfo, agentContext.Email);
            return Result.Success<EdoContracts.Accommodations.Booking, ProblemDetails>(details);
        }


        private async Task<Result<EdoContracts.Accommodations.Booking, ProblemDetails>> GenerateInvoice(EdoContracts.Accommodations.Booking details, string referenceCode, AgentContext agent)
        {
            var (_, isInvoiceFailure, invoiceError) = await _documentsService.GenerateInvoice(referenceCode, agent);
            if (isInvoiceFailure)
                return ProblemDetailsBuilder.Fail<EdoContracts.Accommodations.Booking>(invoiceError);

            return Result.Success<EdoContracts.Accommodations.Booking, ProblemDetails>(details);
        }
        
        public async Task<Result<EdoContracts.Accommodations.Booking, ProblemDetails>> BookOnProvider(Data.Booking.Booking booking, string referenceCode,
            string languageCode, bool withBookingOnSupplier = true)
        {
            // TODO: will be implemented in NIJO-31 
            var bookingRequest = JsonConvert.DeserializeObject<AccommodationBookingRequest>(booking.BookingRequest);

            var features = new List<Feature>(); //bookingRequest.Features

            var roomDetails = bookingRequest.RoomDetails
                .Select(d => new SlimRoomOccupation(d.Type, d.Passengers, string.Empty, d.IsExtraBedNeeded))
                .ToList();

            // return stub details without booking on supplier
            if(!withBookingOnSupplier)
                return GetStubDetails(booking);

            var innerRequest = new BookingRequest(bookingRequest.AvailabilityId,
                bookingRequest.RoomContractSetId,
                booking.ReferenceCode,
                roomDetails,
                features,
                bookingRequest.RejectIfUnavailable);

            try
            {
                var bookingResult = await _supplierConnectorManager
                    .Get(booking.Supplier)
                    .Book(innerRequest, languageCode);

                if (bookingResult.IsSuccess)
                {
                    return bookingResult.Value;
                }

                // If result is failed this does not mean that booking failed. This means that we should check it later.
                _logger.LogBookingFinalizationFailure($"The booking finalization with the reference code: '{referenceCode}' has been failed");
                return GetStubDetails(booking);
            }
            catch
            {
                var errorMessage = $"Failed to update booking data (refcode '{referenceCode}') after the request to the connector";

                var (_, isCancellationFailed, cancellationError) = await _supplierConnectorManager.Get(booking.Supplier).CancelBooking(booking.ReferenceCode);
                if (isCancellationFailed)
                    errorMessage += Environment.NewLine + $"Booking cancellation has failed: {cancellationError}";

                _logger.LogBookingFinalizationFailure(errorMessage);

                return GetStubDetails(booking);
            }


            // TODO: Remove room information and contract description from booking NIJO-915
            static EdoContracts.Accommodations.Booking GetStubDetails(Data.Booking.Booking booking)
                => new EdoContracts.Accommodations.Booking(booking.ReferenceCode,
                    // Will be set in the refresh step
                    BookingStatusCodes.WaitingForResponse,
                    booking.AccommodationId,
                    booking.SupplierReferenceCode,
                    booking.CheckInDate,
                    booking.CheckOutDate,
                    new List<SlimRoomOccupation>(0),
                    BookingUpdateModes.Asynchronous);
        }


        public async Task VoidMoneyAndCancelBooking(Data.Booking.Booking booking, AgentContext agentContext)
        {
            var (_, isFailure, _, error) = await _supplierConnectorManager.Get(booking.Supplier).CancelBooking(booking.ReferenceCode);
            if (isFailure)
            {
                _logger.LogBookingCancelFailure(
                    $"Failed to cancel a booking with reference code '{booking.ReferenceCode}': [{error.Status}] {error.Detail}");

                // We'll refund money only if the booking cancellation was succeeded on supplier
                return;
            }

            var (_, voidOrRefundFailure, voidOrRefundError) = await _paymentService.VoidOrRefund(booking, agentContext.ToUserInfo());
            if (voidOrRefundFailure)
                _logger.LogBookingCancelFailure($"Failure during cancellation of a booking with reference code '{booking.ReferenceCode}':" +
                    $"failed to void or refund money. Error: {voidOrRefundError}");
        }
        
        
        private BookingAvailabilityInfo ExtractBookingAvailabilityInfo(
            (Suppliers Source, DataWithMarkup<RoomContractSetAvailability> Result) responseWithMarkup)
            => ExtractBookingAvailabilityInfo(responseWithMarkup.Source, responseWithMarkup.Result.Data);
        // Temporarily saving availability id along with booking request to get it on the booking step.
        // TODO NIJO-813: Rewrite this to save such data in another place
        
        private BookingAvailabilityInfo ExtractBookingAvailabilityInfo(Suppliers supplier, RoomContractSetAvailability response)
        {
            var location = response.Accommodation.Location;

            return new BookingAvailabilityInfo(
                response.Accommodation.Id,
                response.Accommodation.Name,
                response.RoomContractSet,
                location.LocalityZone,
                location.Locality,
                location.Country,
                location.CountryCode,
                location.Address,
                location.Coordinates,
                response.CheckInDate,
                response.CheckOutDate,
                response.NumberOfNights,
                supplier);
        }
        
        
        private bool AreDeadlineSettingsSuitable(AccommodationBookingRequest bookingRequest, (Suppliers, DataWithMarkup<RoomContractSetAvailability>) bookingData,
            AccommodationBookingSettings settings)
        {
            var (_, dataWithMarkup) = bookingData;
            var deadlineDate = dataWithMarkup.Data.RoomContractSet.Deadline.Date ?? dataWithMarkup.Data.CheckInDate;
            if (deadlineDate.Date > _dateTimeProvider.UtcTomorrow())
                return true;

            return settings.PassedDeadlineOffersMode switch
            {
                PassedDeadlineOffersMode.CardAndAccountPurchases => true,
                PassedDeadlineOffersMode.CardPurchasesOnly
                    when bookingRequest.PaymentMethod == PaymentMethods.CreditCard => true,
                _ => false
            };
        }


        private static bool AreAprSettingsSuitable(AccommodationBookingRequest bookingRequest, (Suppliers, DataWithMarkup<RoomContractSetAvailability>) bookingData,
            AccommodationBookingSettings settings)
        {
            var (_, dataWithMarkup) = bookingData;
            if (!dataWithMarkup.Data.RoomContractSet.IsAdvancePurchaseRate)
                return true;

            return settings.AprMode switch
            {
                AprMode.CardAndAccountPurchases => true,
                AprMode.CardPurchasesOnly
                    when bookingRequest.PaymentMethod == PaymentMethods.CreditCard => true,
                _ => false
            };
        }
        
        
        private async Task<Result<(Suppliers, DataWithMarkup<RoomContractSetAvailability>), ProblemDetails>> GetCachedAvailability(
            AccommodationBookingRequest bookingRequest, AgentContext agentContext)
            => await _bookingEvaluationStorage.Get(bookingRequest.SearchId,
                    bookingRequest.ResultId,
                    bookingRequest.RoomContractSetId,
                    (await _accommodationBookingSettingsService.Get(agentContext)).EnabledConnectors)
                .ToResultWithProblemDetails();
        
        
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IBookingDocumentsService _documentsService;
        private readonly IPaymentNotificationService _notificationService;
        private readonly IBookingMailingService _bookingMailingService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAccountPaymentService _accountPaymentService;
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IBookingPaymentService _paymentService;
        private readonly IBookingEvaluationStorage _bookingEvaluationStorage;
        private readonly EdoContext _context;
        private readonly IBookingResponseProcessor _bookingResponseProcessor;
        private readonly ILogger<BookingRegistrationService> _logger;
    }
}