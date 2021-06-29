using System;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Infrastructure.Logging
{
    public static class LoggerExtensions
    {
        static LoggerExtensions()
        {
            GeoCoderException = LoggerMessage.Define(LogLevel.Error,
                new EventId(1001, "GeoCoderException"),
                "Getting google response exception");
            
            InvitationCreated = LoggerMessage.Define<HappyTravel.Edo.Common.Enums.UserInvitationTypes, string>(LogLevel.Information,
                new EventId(1006, "InvitationCreated"),
                "The invitation with type {InvitationType} created for the user '{Email}'");
            
            AgentRegistrationFailed = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1007, "AgentRegistrationFailed"),
                "Agent registration failed with error `{Error}`");
            
            AgentRegistrationSuccess = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1008, "AgentRegistrationSuccess"),
                "Agent {Email} successfully registered");
            
            PayfortClientException = LoggerMessage.Define(LogLevel.Critical,
                new EventId(1009, "PayfortClientException"),
                "Payfort client exception");
            
            AgencyAccountCreationSuccess = LoggerMessage.Define<int, int>(LogLevel.Information,
                new EventId(1010, "AgencyAccountCreationSuccess"),
                "Successfully created account for agency: '{AgencyId}', account id: {AccountId}");
            
            AgencyAccountCreationFailed = LoggerMessage.Define<int, string>(LogLevel.Error,
                new EventId(1011, "AgencyAccountCreationFailed"),
                "Failed to create account for agency {AgencyId}, error {Error}");
            
            EntityLockFailed = LoggerMessage.Define<string, string>(LogLevel.Critical,
                new EventId(1012, "EntityLockFailed"),
                "Failed to lock entity {EntityType} with id: {EntityId}");
            
            PayfortError = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1013, "PayfortError"),
                "Error deserializing payfort response: '{Content}'");
            
            ExternalPaymentLinkSendSuccess = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1014, "ExternalPaymentLinkSendSuccess"),
                "Successfully sent e-mail to {Email}");
            
            ExternalPaymentLinkSendFailed = LoggerMessage.Define<string, string>(LogLevel.Error,
                new EventId(1015, "ExternalPaymentLinkSendFailed"),
                "Error sending email to {Email}: {Error}");
            
            UnableGetBookingDetailsFromNetstormingXml = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1017, "UnableGetBookingDetailsFromNetstormingXml"),
                "Failed to get booking details from the Netstorming xml: {Xml}");
            
            UnableToAcceptNetstormingRequest = LoggerMessage.Define(LogLevel.Warning,
                new EventId(1018, "UnableToAcceptNetstormingRequest"),
                "Unable to accept netstorming request");
            
            BookingFinalizationFailure = LoggerMessage.Define<string, string>(LogLevel.Error,
                new EventId(1020, "BookingFinalizationFailure"),
                "The booking finalization with the reference code: '{ReferenceCode}' has been failed with a message: {Message}");
            
            BookingFinalizationPaymentFailure = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1021, "BookingFinalizationPaymentFailure"),
                "The booking with reference code: '{ReferenceCode}' hasn't been paid");
            
            BookingFinalizationSuccess = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1022, "BookingFinalizationSuccess"),
                "Successfully booked using account. Reference code: '{ReferenceCode}'");
            
            BookingFinalizationException = LoggerMessage.Define(LogLevel.Critical,
                new EventId(1023, "BookingFinalizationException"),
                "Booking finalization exception");
            
            BookingResponseProcessFailure = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1030, "BookingResponseProcessFailure"),
                "Booking response process failure. Error: {Error}");
            
            BookingResponseProcessSuccess = LoggerMessage.Define<string, string>(LogLevel.Information,
                new EventId(1031, "BookingResponseProcessSuccess"),
                "The booking response with the reference code '{ReferenceCode} has been processed with message {Message}");
            
            BookingResponseProcessStarted = LoggerMessage.Define<string, HappyTravel.Edo.Common.Enums.BookingStatuses>(LogLevel.Information,
                new EventId(1032, "BookingResponseProcessStarted"),
                "Start the booking response processing with the reference code '{ReferenceCode}'. Old status: {Status}");
            
            BookingCancelFailure = LoggerMessage.Define<string, string>(LogLevel.Critical,
                new EventId(1040, "BookingCancelFailure"),
                "Failed to cancel a booking with reference code: '{ReferenceCode}'. Error: {Error}");
            
            BookingCancelSuccess = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1041, "BookingCancelSuccess"),
                "Successfully cancelled a booking with reference code: '{ReferenceCode}'");
            
            BookingAlreadyCancelled = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1042, "BookingAlreadyCancelled"),
                "Skipping cancellation for a booking with reference code: '{ReferenceCode}'. Already cancelled.");
            
            BookingRegistrationSuccess = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1050, "BookingRegistrationSuccess"),
                "Successfully registered a booking with reference code: '{ReferenceCode}");
            
            BookingRegistrationFailure = LoggerMessage.Define<System.Guid, string, string, string>(LogLevel.Error,
                new EventId(1051, "BookingRegistrationFailure"),
                "Failed to register a booking. AvailabilityId: '{AvailabilityId}'. Itinerary number: {ItineraryNumber}. Passenger name: {MainPassengerName}. Error: {Error}");
            
            BookingByAccountSuccess = LoggerMessage.Define(LogLevel.Information,
                new EventId(1060, "BookingByAccountSuccess"),
                "Booking by account success");
            
            BookingByAccountFailure = LoggerMessage.Define<string, string>(LogLevel.Error,
                new EventId(1061, "BookingByAccountFailure"),
                "Failed to book using account. Reference code: '{ReferenceCode}'. Error: {Error}");
            
            BookingRefreshStatusSuccess = LoggerMessage.Define<string, HappyTravel.Edo.Common.Enums.BookingStatuses, HappyTravel.EdoContracts.Accommodations.Enums.BookingStatusCodes>(LogLevel.Information,
                new EventId(1070, "BookingRefreshStatusSuccess"),
                "Successfully refreshed status for a booking with reference code: '{ReferenceCode}'. Old status: {OldStatus}. New status: {Status}");
            
            BookingRefreshStatusFailure = LoggerMessage.Define<string, string>(LogLevel.Error,
                new EventId(1071, "BookingRefreshStatusFailure"),
                "Failed to refresh status for a booking with reference code: '{ReferenceCode}' while getting info from a supplier. Error: {Error}");
            
            BookingConfirmationFailure = LoggerMessage.Define<string, string>(LogLevel.Critical,
                new EventId(1072, "BookingConfirmationFailure"),
                "Booking '{ReferenceCode} confirmation failed: '{Error}");
            
            BookingEvaluationFailure = LoggerMessage.Define<System.Nullable<int>, string>(LogLevel.Critical,
                new EventId(1073, "BookingEvaluationFailure"),
                "EvaluateOnConnector returned status code: {Status}, error: {Error}");
            
            ExternalAdministratorAuthorizationSuccess = LoggerMessage.Define(LogLevel.Debug,
                new EventId(1100, "ExternalAdministratorAuthorizationSuccess"),
                "Successfully authorized external administrator");
            
            AdministratorAuthorizationFailure = LoggerMessage.Define(LogLevel.Warning,
                new EventId(1101, "AdministratorAuthorizationFailure"),
                "Administrator authorization failed");
            
            InternalAdministratorAuthorizationSuccess = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1103, "InternalAdministratorAuthorizationSuccess"),
                "Successfully authorized administrator '{Email}'");
            
            AgentAuthorizationSuccess = LoggerMessage.Define<string, string>(LogLevel.Debug,
                new EventId(1110, "AgentAuthorizationSuccess"),
                "Successfully authorized agent '{Email}' for '{Permissions}'");
            
            AgentAuthorizationFailure = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1111, "AgentAuthorizationFailure"),
                "Agent authorization failure: '{Error}'");
            
            CounterpartyAccountCreationFailure = LoggerMessage.Define<int, string>(LogLevel.Error,
                new EventId(1120, "CounterpartyAccountCreationFailure"),
                "Failed to create account for counterparty {Id}, error {Error}");
            
            CounterpartyAccountCreationSuccess = LoggerMessage.Define<int, int>(LogLevel.Information,
                new EventId(1121, "CounterpartyAccountCreationSuccess"),
                "Successfully created account for counterparty: '{CounterpartyId}', account id: {AccountId}");
            
            ServiceAccountAuthorizationSuccess = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1125, "ServiceAccountAuthorizationSuccess"),
                "Service account '{ClientId}' authorized successfully");
            
            ServiceAccountAuthorizationFailure = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1126, "ServiceAccountAuthorizationFailure"),
                "Service account authorization failed. Error: {Error}");
            
            LocationNormalized = LoggerMessage.Define(LogLevel.Information,
                new EventId(1130, "LocationNormalized"),
                "Location normalized");
            
            MultiProviderAvailabilitySearchStarted = LoggerMessage.Define<System.Guid>(LogLevel.Information,
                new EventId(1140, "MultiProviderAvailabilitySearchStarted"),
                "Starting availability search with id '{SearchId}'");
            
            ProviderAvailabilitySearchStarted = LoggerMessage.Define<System.Guid, HappyTravel.SuppliersCatalog.Suppliers>(LogLevel.Debug,
                new EventId(1141, "ProviderAvailabilitySearchStarted"),
                "Availability search with id '{SearchId}' on supplier '{Supplier}' started");
            
            ProviderAvailabilitySearchSuccess = LoggerMessage.Define<System.Guid, HappyTravel.SuppliersCatalog.Suppliers, int>(LogLevel.Debug,
                new EventId(1142, "ProviderAvailabilitySearchSuccess"),
                "Availability search with id '{SearchId}' on supplier '{Supplier}' finished successfully with '{ResultCount}' results");
            
            ProviderAvailabilitySearchFailure = LoggerMessage.Define<System.Guid, HappyTravel.SuppliersCatalog.Suppliers, HappyTravel.Edo.Api.Models.Availabilities.AvailabilitySearchTaskState, string>(LogLevel.Error,
                new EventId(1143, "ProviderAvailabilitySearchFailure"),
                "Availability search with id '{SearchId}' on supplier '{Supplier}' finished with state '{TaskState}', error '{Error}'");
            
            ProviderAvailabilitySearchException = LoggerMessage.Define(LogLevel.Critical,
                new EventId(1145, "ProviderAvailabilitySearchException"),
                "Provider availability search exception");
            
            CounterpartyStateAuthorizationSuccess = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1150, "CounterpartyStateAuthorizationSuccess"),
                "Successfully checked counterparty state for agent {Email}");
            
            CounterpartyStateAuthorizationFailure = LoggerMessage.Define<string, HappyTravel.Edo.Common.Enums.CounterpartyStates>(LogLevel.Warning,
                new EventId(1151, "CounterpartyStateAuthorizationFailure"),
                "Counterparty of agent '{Email}' has wrong state {State}");
            
            DefaultLanguageKeyIsMissingInFieldOfLocationsTable = LoggerMessage.Define(LogLevel.Warning,
                new EventId(1200, "DefaultLanguageKeyIsMissingInFieldOfLocationsTable"),
                "Default language key is missing in field of locations table");
            
            ConnectorClientException = LoggerMessage.Define(LogLevel.Critical,
                new EventId(1300, "ConnectorClientException"),
                "Connector client exception");
            
            SupplierConnectorRequestError = LoggerMessage.Define<string, string, System.Nullable<int>>(LogLevel.Error,
                new EventId(1301, "SupplierConnectorRequestError"),
                "Error executing connector request to {Url}: '{Error}', status code: '{Status}'");
            
            SupplierConnectorRequestDuration = LoggerMessage.Define<string, long>(LogLevel.Information,
                new EventId(1302, "SupplierConnectorRequestDuration"),
                "Request to {Url} finished at {ElapsedMilliseconds} ms.");
            
            GetTokenForConnectorError = LoggerMessage.Define<string, string, System.DateTime>(LogLevel.Error,
                new EventId(1310, "GetTokenForConnectorError"),
                "Something went wrong while requesting the access token. Error: {Error}. Using existing token: '{Token}' with expiry date '{ExpiryDate}'");
            
            UnauthorizedConnectorResponse = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1311, "UnauthorizedConnectorResponse"),
                "Unauthorized response was returned from '{RequestUri}'. Refreshing token...");
            
            CaptureMoneyForBookingSuccess = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1400, "CaptureMoneyForBookingSuccess"),
                "Successfully captured money for a booking with reference code: '{ReferenceCode}'");
            
            CaptureMoneyForBookingFailure = LoggerMessage.Define<string, HappyTravel.Edo.Common.Enums.PaymentTypes>(LogLevel.Error,
                new EventId(1401, "CaptureMoneyForBookingFailure"),
                "Failed to capture money for a booking with reference code: '{ReferenceCode}'. Error: Invalid payment method: {PaymentType}");
            
            ChargeMoneyForBookingSuccess = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1402, "ChargeMoneyForBookingSuccess"),
                "Successfully charged money for a booking with reference code: '{ReferenceCode}'");
            
            ChargeMoneyForBookingFailure = LoggerMessage.Define<string, string>(LogLevel.Error,
                new EventId(1403, "ChargeMoneyForBookingFailure"),
                "Failed to charge money for a booking with reference code: '{ReferenceCode} with error {Error}");
            
            ProcessPaymentChangesForBookingSuccess = LoggerMessage.Define<HappyTravel.Edo.Common.Enums.BookingPaymentStatuses, HappyTravel.Edo.Common.Enums.PaymentStatuses, string, string>(LogLevel.Information,
                new EventId(1410, "ProcessPaymentChangesForBookingSuccess"),
                "Successfully processes payment changes. Old payment status: {OldPaymentStatus}. New payment status: {PaymentStatus}. Payment: '{PaymentReferenceCode}'. Booking reference code: '{BookingReferenceCode}'");
            
            ProcessPaymentChangesForBookingSkip = LoggerMessage.Define<HappyTravel.Edo.Common.Enums.PaymentStatuses, string, string>(LogLevel.Warning,
                new EventId(1411, "ProcessPaymentChangesForBookingSkip"),
                "Skipped booking status update while processing payment changes. Payment status: {PaymentStatus}. Payment: '{PaymentReferenceCode}'. Booking reference code: '{BookingReferenceCode}'");
            
            ProcessPaymentChangesForBookingFailure = LoggerMessage.Define<HappyTravel.Edo.Common.Enums.PaymentStatuses, string>(LogLevel.Error,
                new EventId(1412, "ProcessPaymentChangesForBookingFailure"),
                "Failed to process payment changes, could not find the corresponding booking. Payment status: {Status}. Payment: '{ReferenceCode}'");
            
            ElasticAnalyticsEventSendError = LoggerMessage.Define(LogLevel.Error,
                new EventId(1501, "ElasticAnalyticsEventSendError"),
                "Sending event to elasticsearch failed");
            
            MapperClientException = LoggerMessage.Define(LogLevel.Error,
                new EventId(1601, "MapperClientException"),
                "Mapper client exception");
            
            CounterpartyAccountAddedNotificationFailure = LoggerMessage.Define<int, string>(LogLevel.Error,
                new EventId(1701, "CounterpartyAccountAddedNotificationFailure"),
                "Counterparty {CounterpartyId} account added notification failed with error {Error}");
            
            AgentRegistrationNotificationFailure = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1702, "AgentRegistrationNotificationFailure"),
                "Agent registration notification failure with error {Error}");
            
            ChildAgencyRegistrationNotificationFailure = LoggerMessage.Define(LogLevel.Error,
                new EventId(1703, "ChildAgencyRegistrationNotificationFailure"),
                "Child agency registration notification failed");
            
            CounterpartyAccountSubtractedNotificationFailure = LoggerMessage.Define<int, string>(LogLevel.Error,
                new EventId(1704, "CounterpartyAccountSubtractedNotificationFailure"),
                "Counterparty {CounterpartyId} account subtracted notification failed with error {Error}");
            
            CounterpartyAccountIncreasedManuallyNotificationFailure = LoggerMessage.Define<int, string>(LogLevel.Error,
                new EventId(1705, "CounterpartyAccountIncreasedManuallyNotificationFailure"),
                "Counterparty {CounterpartyId} account increasedManually notification failed with error {Error}");
            
            CounterpartyAccountDecreasedManuallyNotificationFailure = LoggerMessage.Define<int, string>(LogLevel.Error,
                new EventId(1706, "CounterpartyAccountDecreasedManuallyNotificationFailure"),
                "Counterparty {CounterpartyId} account decreasedManually notification failed with error {Error}");
            
            ExternalPaymentLinkGenerationSuccess = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1707, "ExternalPaymentLinkGenerationSuccess"),
                "Successfully generated payment link for {Email}");
            
            ExternalPaymentLinkGenerationFailed = LoggerMessage.Define<string, string>(LogLevel.Error,
                new EventId(1708, "ExternalPaymentLinkGenerationFailed"),
                "Error generating payment link for {Email}: {Error}");
            
            GetAccommodationByHtIdFailed = LoggerMessage.Define<string, string>(LogLevel.Error,
                new EventId(1709, "GetAccommodationByHtIdFailed"),
                "Error getting accommodation for HtId '{HtId}': error: {Error}");
            
        }
    
                
         public static void LogGeoCoderException(this ILogger logger, Exception exception = null)
            => GeoCoderException(logger, exception);
                
         public static void LogInvitationCreated(this ILogger logger, HappyTravel.Edo.Common.Enums.UserInvitationTypes InvitationType, string Email, Exception exception = null)
            => InvitationCreated(logger, InvitationType, Email, exception);
                
         public static void LogAgentRegistrationFailed(this ILogger logger, string Error, Exception exception = null)
            => AgentRegistrationFailed(logger, Error, exception);
                
         public static void LogAgentRegistrationSuccess(this ILogger logger, string Email, Exception exception = null)
            => AgentRegistrationSuccess(logger, Email, exception);
                
         public static void LogPayfortClientException(this ILogger logger, Exception exception = null)
            => PayfortClientException(logger, exception);
                
         public static void LogAgencyAccountCreationSuccess(this ILogger logger, int AgencyId, int AccountId, Exception exception = null)
            => AgencyAccountCreationSuccess(logger, AgencyId, AccountId, exception);
                
         public static void LogAgencyAccountCreationFailed(this ILogger logger, int AgencyId, string Error, Exception exception = null)
            => AgencyAccountCreationFailed(logger, AgencyId, Error, exception);
                
         public static void LogEntityLockFailed(this ILogger logger, string EntityType, string EntityId, Exception exception = null)
            => EntityLockFailed(logger, EntityType, EntityId, exception);
                
         public static void LogPayfortError(this ILogger logger, string Content, Exception exception = null)
            => PayfortError(logger, Content, exception);
                
         public static void LogExternalPaymentLinkSendSuccess(this ILogger logger, string Email, Exception exception = null)
            => ExternalPaymentLinkSendSuccess(logger, Email, exception);
                
         public static void LogExternalPaymentLinkSendFailed(this ILogger logger, string Email, string Error, Exception exception = null)
            => ExternalPaymentLinkSendFailed(logger, Email, Error, exception);
                
         public static void LogUnableGetBookingDetailsFromNetstormingXml(this ILogger logger, string Xml, Exception exception = null)
            => UnableGetBookingDetailsFromNetstormingXml(logger, Xml, exception);
                
         public static void LogUnableToAcceptNetstormingRequest(this ILogger logger, Exception exception = null)
            => UnableToAcceptNetstormingRequest(logger, exception);
                
         public static void LogBookingFinalizationFailure(this ILogger logger, string ReferenceCode, string Message, Exception exception = null)
            => BookingFinalizationFailure(logger, ReferenceCode, Message, exception);
                
         public static void LogBookingFinalizationPaymentFailure(this ILogger logger, string ReferenceCode, Exception exception = null)
            => BookingFinalizationPaymentFailure(logger, ReferenceCode, exception);
                
         public static void LogBookingFinalizationSuccess(this ILogger logger, string ReferenceCode, Exception exception = null)
            => BookingFinalizationSuccess(logger, ReferenceCode, exception);
                
         public static void LogBookingFinalizationException(this ILogger logger, Exception exception = null)
            => BookingFinalizationException(logger, exception);
                
         public static void LogBookingResponseProcessFailure(this ILogger logger, string Error, Exception exception = null)
            => BookingResponseProcessFailure(logger, Error, exception);
                
         public static void LogBookingResponseProcessSuccess(this ILogger logger, string ReferenceCode, string Message, Exception exception = null)
            => BookingResponseProcessSuccess(logger, ReferenceCode, Message, exception);
                
         public static void LogBookingResponseProcessStarted(this ILogger logger, string ReferenceCode, HappyTravel.Edo.Common.Enums.BookingStatuses Status, Exception exception = null)
            => BookingResponseProcessStarted(logger, ReferenceCode, Status, exception);
                
         public static void LogBookingCancelFailure(this ILogger logger, string ReferenceCode, string Error, Exception exception = null)
            => BookingCancelFailure(logger, ReferenceCode, Error, exception);
                
         public static void LogBookingCancelSuccess(this ILogger logger, string ReferenceCode, Exception exception = null)
            => BookingCancelSuccess(logger, ReferenceCode, exception);
                
         public static void LogBookingAlreadyCancelled(this ILogger logger, string ReferenceCode, Exception exception = null)
            => BookingAlreadyCancelled(logger, ReferenceCode, exception);
                
         public static void LogBookingRegistrationSuccess(this ILogger logger, string ReferenceCode, Exception exception = null)
            => BookingRegistrationSuccess(logger, ReferenceCode, exception);
                
         public static void LogBookingRegistrationFailure(this ILogger logger, System.Guid AvailabilityId, string ItineraryNumber, string MainPassengerName, string Error, Exception exception = null)
            => BookingRegistrationFailure(logger, AvailabilityId, ItineraryNumber, MainPassengerName, Error, exception);
                
         public static void LogBookingByAccountSuccess(this ILogger logger, Exception exception = null)
            => BookingByAccountSuccess(logger, exception);
                
         public static void LogBookingByAccountFailure(this ILogger logger, string ReferenceCode, string Error, Exception exception = null)
            => BookingByAccountFailure(logger, ReferenceCode, Error, exception);
                
         public static void LogBookingRefreshStatusSuccess(this ILogger logger, string ReferenceCode, HappyTravel.Edo.Common.Enums.BookingStatuses OldStatus, HappyTravel.EdoContracts.Accommodations.Enums.BookingStatusCodes Status, Exception exception = null)
            => BookingRefreshStatusSuccess(logger, ReferenceCode, OldStatus, Status, exception);
                
         public static void LogBookingRefreshStatusFailure(this ILogger logger, string ReferenceCode, string Error, Exception exception = null)
            => BookingRefreshStatusFailure(logger, ReferenceCode, Error, exception);
                
         public static void LogBookingConfirmationFailure(this ILogger logger, string ReferenceCode, string Error, Exception exception = null)
            => BookingConfirmationFailure(logger, ReferenceCode, Error, exception);
                
         public static void LogBookingEvaluationFailure(this ILogger logger, System.Nullable<int> Status, string Error, Exception exception = null)
            => BookingEvaluationFailure(logger, Status, Error, exception);
                
         public static void LogExternalAdministratorAuthorizationSuccess(this ILogger logger, Exception exception = null)
            => ExternalAdministratorAuthorizationSuccess(logger, exception);
                
         public static void LogAdministratorAuthorizationFailure(this ILogger logger, Exception exception = null)
            => AdministratorAuthorizationFailure(logger, exception);
                
         public static void LogInternalAdministratorAuthorizationSuccess(this ILogger logger, string Email, Exception exception = null)
            => InternalAdministratorAuthorizationSuccess(logger, Email, exception);
                
         public static void LogAgentAuthorizationSuccess(this ILogger logger, string Email, string Permissions, Exception exception = null)
            => AgentAuthorizationSuccess(logger, Email, Permissions, exception);
                
         public static void LogAgentAuthorizationFailure(this ILogger logger, string Error, Exception exception = null)
            => AgentAuthorizationFailure(logger, Error, exception);
                
         public static void LogCounterpartyAccountCreationFailure(this ILogger logger, int Id, string Error, Exception exception = null)
            => CounterpartyAccountCreationFailure(logger, Id, Error, exception);
                
         public static void LogCounterpartyAccountCreationSuccess(this ILogger logger, int CounterpartyId, int AccountId, Exception exception = null)
            => CounterpartyAccountCreationSuccess(logger, CounterpartyId, AccountId, exception);
                
         public static void LogServiceAccountAuthorizationSuccess(this ILogger logger, string ClientId, Exception exception = null)
            => ServiceAccountAuthorizationSuccess(logger, ClientId, exception);
                
         public static void LogServiceAccountAuthorizationFailure(this ILogger logger, string Error, Exception exception = null)
            => ServiceAccountAuthorizationFailure(logger, Error, exception);
                
         public static void LogLocationNormalized(this ILogger logger, Exception exception = null)
            => LocationNormalized(logger, exception);
                
         public static void LogMultiProviderAvailabilitySearchStarted(this ILogger logger, System.Guid SearchId, Exception exception = null)
            => MultiProviderAvailabilitySearchStarted(logger, SearchId, exception);
                
         public static void LogProviderAvailabilitySearchStarted(this ILogger logger, System.Guid SearchId, HappyTravel.SuppliersCatalog.Suppliers Supplier, Exception exception = null)
            => ProviderAvailabilitySearchStarted(logger, SearchId, Supplier, exception);
                
         public static void LogProviderAvailabilitySearchSuccess(this ILogger logger, System.Guid SearchId, HappyTravel.SuppliersCatalog.Suppliers Supplier, int ResultCount, Exception exception = null)
            => ProviderAvailabilitySearchSuccess(logger, SearchId, Supplier, ResultCount, exception);
                
         public static void LogProviderAvailabilitySearchFailure(this ILogger logger, System.Guid SearchId, HappyTravel.SuppliersCatalog.Suppliers Supplier, HappyTravel.Edo.Api.Models.Availabilities.AvailabilitySearchTaskState TaskState, string Error, Exception exception = null)
            => ProviderAvailabilitySearchFailure(logger, SearchId, Supplier, TaskState, Error, exception);
                
         public static void LogProviderAvailabilitySearchException(this ILogger logger, Exception exception = null)
            => ProviderAvailabilitySearchException(logger, exception);
                
         public static void LogCounterpartyStateAuthorizationSuccess(this ILogger logger, string Email, Exception exception = null)
            => CounterpartyStateAuthorizationSuccess(logger, Email, exception);
                
         public static void LogCounterpartyStateAuthorizationFailure(this ILogger logger, string Email, HappyTravel.Edo.Common.Enums.CounterpartyStates State, Exception exception = null)
            => CounterpartyStateAuthorizationFailure(logger, Email, State, exception);
                
         public static void LogDefaultLanguageKeyIsMissingInFieldOfLocationsTable(this ILogger logger, Exception exception = null)
            => DefaultLanguageKeyIsMissingInFieldOfLocationsTable(logger, exception);
                
         public static void LogConnectorClientException(this ILogger logger, Exception exception = null)
            => ConnectorClientException(logger, exception);
                
         public static void LogSupplierConnectorRequestError(this ILogger logger, string Url, string Error, System.Nullable<int> Status, Exception exception = null)
            => SupplierConnectorRequestError(logger, Url, Error, Status, exception);
                
         public static void LogSupplierConnectorRequestDuration(this ILogger logger, string Url, long ElapsedMilliseconds, Exception exception = null)
            => SupplierConnectorRequestDuration(logger, Url, ElapsedMilliseconds, exception);
                
         public static void LogGetTokenForConnectorError(this ILogger logger, string Error, string Token, System.DateTime ExpiryDate, Exception exception = null)
            => GetTokenForConnectorError(logger, Error, Token, ExpiryDate, exception);
                
         public static void LogUnauthorizedConnectorResponse(this ILogger logger, string RequestUri, Exception exception = null)
            => UnauthorizedConnectorResponse(logger, RequestUri, exception);
                
         public static void LogCaptureMoneyForBookingSuccess(this ILogger logger, string ReferenceCode, Exception exception = null)
            => CaptureMoneyForBookingSuccess(logger, ReferenceCode, exception);
                
         public static void LogCaptureMoneyForBookingFailure(this ILogger logger, string ReferenceCode, HappyTravel.Edo.Common.Enums.PaymentTypes PaymentType, Exception exception = null)
            => CaptureMoneyForBookingFailure(logger, ReferenceCode, PaymentType, exception);
                
         public static void LogChargeMoneyForBookingSuccess(this ILogger logger, string ReferenceCode, Exception exception = null)
            => ChargeMoneyForBookingSuccess(logger, ReferenceCode, exception);
                
         public static void LogChargeMoneyForBookingFailure(this ILogger logger, string ReferenceCode, string Error, Exception exception = null)
            => ChargeMoneyForBookingFailure(logger, ReferenceCode, Error, exception);
                
         public static void LogProcessPaymentChangesForBookingSuccess(this ILogger logger, HappyTravel.Edo.Common.Enums.BookingPaymentStatuses OldPaymentStatus, HappyTravel.Edo.Common.Enums.PaymentStatuses PaymentStatus, string PaymentReferenceCode, string BookingReferenceCode, Exception exception = null)
            => ProcessPaymentChangesForBookingSuccess(logger, OldPaymentStatus, PaymentStatus, PaymentReferenceCode, BookingReferenceCode, exception);
                
         public static void LogProcessPaymentChangesForBookingSkip(this ILogger logger, HappyTravel.Edo.Common.Enums.PaymentStatuses PaymentStatus, string PaymentReferenceCode, string BookingReferenceCode, Exception exception = null)
            => ProcessPaymentChangesForBookingSkip(logger, PaymentStatus, PaymentReferenceCode, BookingReferenceCode, exception);
                
         public static void LogProcessPaymentChangesForBookingFailure(this ILogger logger, HappyTravel.Edo.Common.Enums.PaymentStatuses Status, string ReferenceCode, Exception exception = null)
            => ProcessPaymentChangesForBookingFailure(logger, Status, ReferenceCode, exception);
                
         public static void LogElasticAnalyticsEventSendError(this ILogger logger, Exception exception = null)
            => ElasticAnalyticsEventSendError(logger, exception);
                
         public static void LogMapperClientException(this ILogger logger, Exception exception = null)
            => MapperClientException(logger, exception);
                
         public static void LogCounterpartyAccountAddedNotificationFailure(this ILogger logger, int CounterpartyId, string Error, Exception exception = null)
            => CounterpartyAccountAddedNotificationFailure(logger, CounterpartyId, Error, exception);
                
         public static void LogAgentRegistrationNotificationFailure(this ILogger logger, string Error, Exception exception = null)
            => AgentRegistrationNotificationFailure(logger, Error, exception);
                
         public static void LogChildAgencyRegistrationNotificationFailure(this ILogger logger, Exception exception = null)
            => ChildAgencyRegistrationNotificationFailure(logger, exception);
                
         public static void LogCounterpartyAccountSubtractedNotificationFailure(this ILogger logger, int CounterpartyId, string Error, Exception exception = null)
            => CounterpartyAccountSubtractedNotificationFailure(logger, CounterpartyId, Error, exception);
                
         public static void LogCounterpartyAccountIncreasedManuallyNotificationFailure(this ILogger logger, int CounterpartyId, string Error, Exception exception = null)
            => CounterpartyAccountIncreasedManuallyNotificationFailure(logger, CounterpartyId, Error, exception);
                
         public static void LogCounterpartyAccountDecreasedManuallyNotificationFailure(this ILogger logger, int CounterpartyId, string Error, Exception exception = null)
            => CounterpartyAccountDecreasedManuallyNotificationFailure(logger, CounterpartyId, Error, exception);
                
         public static void LogExternalPaymentLinkGenerationSuccess(this ILogger logger, string Email, Exception exception = null)
            => ExternalPaymentLinkGenerationSuccess(logger, Email, exception);
                
         public static void LogExternalPaymentLinkGenerationFailed(this ILogger logger, string Email, string Error, Exception exception = null)
            => ExternalPaymentLinkGenerationFailed(logger, Email, Error, exception);
                
         public static void LogGetAccommodationByHtIdFailed(this ILogger logger, string HtId, string Error, Exception exception = null)
            => GetAccommodationByHtIdFailed(logger, HtId, Error, exception);
    
    
        
        private static readonly Action<ILogger, Exception> GeoCoderException;
        
        private static readonly Action<ILogger, HappyTravel.Edo.Common.Enums.UserInvitationTypes, string, Exception> InvitationCreated;
        
        private static readonly Action<ILogger, string, Exception> AgentRegistrationFailed;
        
        private static readonly Action<ILogger, string, Exception> AgentRegistrationSuccess;
        
        private static readonly Action<ILogger, Exception> PayfortClientException;
        
        private static readonly Action<ILogger, int, int, Exception> AgencyAccountCreationSuccess;
        
        private static readonly Action<ILogger, int, string, Exception> AgencyAccountCreationFailed;
        
        private static readonly Action<ILogger, string, string, Exception> EntityLockFailed;
        
        private static readonly Action<ILogger, string, Exception> PayfortError;
        
        private static readonly Action<ILogger, string, Exception> ExternalPaymentLinkSendSuccess;
        
        private static readonly Action<ILogger, string, string, Exception> ExternalPaymentLinkSendFailed;
        
        private static readonly Action<ILogger, string, Exception> UnableGetBookingDetailsFromNetstormingXml;
        
        private static readonly Action<ILogger, Exception> UnableToAcceptNetstormingRequest;
        
        private static readonly Action<ILogger, string, string, Exception> BookingFinalizationFailure;
        
        private static readonly Action<ILogger, string, Exception> BookingFinalizationPaymentFailure;
        
        private static readonly Action<ILogger, string, Exception> BookingFinalizationSuccess;
        
        private static readonly Action<ILogger, Exception> BookingFinalizationException;
        
        private static readonly Action<ILogger, string, Exception> BookingResponseProcessFailure;
        
        private static readonly Action<ILogger, string, string, Exception> BookingResponseProcessSuccess;
        
        private static readonly Action<ILogger, string, HappyTravel.Edo.Common.Enums.BookingStatuses, Exception> BookingResponseProcessStarted;
        
        private static readonly Action<ILogger, string, string, Exception> BookingCancelFailure;
        
        private static readonly Action<ILogger, string, Exception> BookingCancelSuccess;
        
        private static readonly Action<ILogger, string, Exception> BookingAlreadyCancelled;
        
        private static readonly Action<ILogger, string, Exception> BookingRegistrationSuccess;
        
        private static readonly Action<ILogger, System.Guid, string, string, string, Exception> BookingRegistrationFailure;
        
        private static readonly Action<ILogger, Exception> BookingByAccountSuccess;
        
        private static readonly Action<ILogger, string, string, Exception> BookingByAccountFailure;
        
        private static readonly Action<ILogger, string, HappyTravel.Edo.Common.Enums.BookingStatuses, HappyTravel.EdoContracts.Accommodations.Enums.BookingStatusCodes, Exception> BookingRefreshStatusSuccess;
        
        private static readonly Action<ILogger, string, string, Exception> BookingRefreshStatusFailure;
        
        private static readonly Action<ILogger, string, string, Exception> BookingConfirmationFailure;
        
        private static readonly Action<ILogger, System.Nullable<int>, string, Exception> BookingEvaluationFailure;
        
        private static readonly Action<ILogger, Exception> ExternalAdministratorAuthorizationSuccess;
        
        private static readonly Action<ILogger, Exception> AdministratorAuthorizationFailure;
        
        private static readonly Action<ILogger, string, Exception> InternalAdministratorAuthorizationSuccess;
        
        private static readonly Action<ILogger, string, string, Exception> AgentAuthorizationSuccess;
        
        private static readonly Action<ILogger, string, Exception> AgentAuthorizationFailure;
        
        private static readonly Action<ILogger, int, string, Exception> CounterpartyAccountCreationFailure;
        
        private static readonly Action<ILogger, int, int, Exception> CounterpartyAccountCreationSuccess;
        
        private static readonly Action<ILogger, string, Exception> ServiceAccountAuthorizationSuccess;
        
        private static readonly Action<ILogger, string, Exception> ServiceAccountAuthorizationFailure;
        
        private static readonly Action<ILogger, Exception> LocationNormalized;
        
        private static readonly Action<ILogger, System.Guid, Exception> MultiProviderAvailabilitySearchStarted;
        
        private static readonly Action<ILogger, System.Guid, HappyTravel.SuppliersCatalog.Suppliers, Exception> ProviderAvailabilitySearchStarted;
        
        private static readonly Action<ILogger, System.Guid, HappyTravel.SuppliersCatalog.Suppliers, int, Exception> ProviderAvailabilitySearchSuccess;
        
        private static readonly Action<ILogger, System.Guid, HappyTravel.SuppliersCatalog.Suppliers, HappyTravel.Edo.Api.Models.Availabilities.AvailabilitySearchTaskState, string, Exception> ProviderAvailabilitySearchFailure;
        
        private static readonly Action<ILogger, Exception> ProviderAvailabilitySearchException;
        
        private static readonly Action<ILogger, string, Exception> CounterpartyStateAuthorizationSuccess;
        
        private static readonly Action<ILogger, string, HappyTravel.Edo.Common.Enums.CounterpartyStates, Exception> CounterpartyStateAuthorizationFailure;
        
        private static readonly Action<ILogger, Exception> DefaultLanguageKeyIsMissingInFieldOfLocationsTable;
        
        private static readonly Action<ILogger, Exception> ConnectorClientException;
        
        private static readonly Action<ILogger, string, string, System.Nullable<int>, Exception> SupplierConnectorRequestError;
        
        private static readonly Action<ILogger, string, long, Exception> SupplierConnectorRequestDuration;
        
        private static readonly Action<ILogger, string, string, System.DateTime, Exception> GetTokenForConnectorError;
        
        private static readonly Action<ILogger, string, Exception> UnauthorizedConnectorResponse;
        
        private static readonly Action<ILogger, string, Exception> CaptureMoneyForBookingSuccess;
        
        private static readonly Action<ILogger, string, HappyTravel.Edo.Common.Enums.PaymentTypes, Exception> CaptureMoneyForBookingFailure;
        
        private static readonly Action<ILogger, string, Exception> ChargeMoneyForBookingSuccess;
        
        private static readonly Action<ILogger, string, string, Exception> ChargeMoneyForBookingFailure;
        
        private static readonly Action<ILogger, HappyTravel.Edo.Common.Enums.BookingPaymentStatuses, HappyTravel.Edo.Common.Enums.PaymentStatuses, string, string, Exception> ProcessPaymentChangesForBookingSuccess;
        
        private static readonly Action<ILogger, HappyTravel.Edo.Common.Enums.PaymentStatuses, string, string, Exception> ProcessPaymentChangesForBookingSkip;
        
        private static readonly Action<ILogger, HappyTravel.Edo.Common.Enums.PaymentStatuses, string, Exception> ProcessPaymentChangesForBookingFailure;
        
        private static readonly Action<ILogger, Exception> ElasticAnalyticsEventSendError;
        
        private static readonly Action<ILogger, Exception> MapperClientException;
        
        private static readonly Action<ILogger, int, string, Exception> CounterpartyAccountAddedNotificationFailure;
        
        private static readonly Action<ILogger, string, Exception> AgentRegistrationNotificationFailure;
        
        private static readonly Action<ILogger, Exception> ChildAgencyRegistrationNotificationFailure;
        
        private static readonly Action<ILogger, int, string, Exception> CounterpartyAccountSubtractedNotificationFailure;
        
        private static readonly Action<ILogger, int, string, Exception> CounterpartyAccountIncreasedManuallyNotificationFailure;
        
        private static readonly Action<ILogger, int, string, Exception> CounterpartyAccountDecreasedManuallyNotificationFailure;
        
        private static readonly Action<ILogger, string, Exception> ExternalPaymentLinkGenerationSuccess;
        
        private static readonly Action<ILogger, string, string, Exception> ExternalPaymentLinkGenerationFailed;
        
        private static readonly Action<ILogger, string, string, Exception> GetAccommodationByHtIdFailed;
    }
}