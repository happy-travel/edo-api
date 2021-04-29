using System;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Infrastructure.Logging
{
    public static class LoggerExtensions
    {
        static LoggerExtensions()
        {
            GeoCoderExceptionOccured = LoggerMessage.Define(LogLevel.Error,
                new EventId(1001, "GeoCoderException"),
                $"ERROR | GoogleGeoCoder: ");
            
            InvitationCreatedOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1006, "InvitationCreated"),
                $"INFORMATION | AgentInvitationService: {{message}}");
            
            AgentRegistrationFailedOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1007, "AgentRegistrationFailed"),
                $"WARNING | AgentRegistrationService: {{message}}");
            
            AgentRegistrationSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1008, "AgentRegistrationSuccess"),
                $"INFORMATION | AgentRegistrationService: {{message}}");
            
            PayfortClientExceptionOccured = LoggerMessage.Define(LogLevel.Critical,
                new EventId(1009, "PayfortClientException"),
                $"CRITICAL | PayfortService: ");
            
            AgencyAccountCreationSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1010, "AgencyAccountCreationSuccess"),
                $"INFORMATION | AccountManagementService: {{message}}");
            
            AgencyAccountCreationFailedOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1011, "AgencyAccountCreationFailed"),
                $"ERROR | AccountManagementService: {{message}}");
            
            EntityLockFailedOccured = LoggerMessage.Define<string>(LogLevel.Critical,
                new EventId(1012, "EntityLockFailed"),
                $"CRITICAL | EntityLocker: {{message}}");
            
            PayfortErrorOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1013, "PayfortError"),
                $"ERROR | PayfortService: {{message}}");
            
            ExternalPaymentLinkSendSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1014, "ExternalPaymentLinkSendSuccess"),
                $"INFORMATION | PaymentLinkService: {{message}}");
            
            ExternalPaymentLinkSendFailedOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1015, "ExternalPaymentLinkSendFailed"),
                $"ERROR | PaymentLinkService: {{message}}");
            
            UnableGetBookingDetailsFromNetstormingXmlOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1017, "UnableGetBookingDetailsFromNetstormingXml"),
                $"WARNING | Test: {{message}}");
            
            UnableToAcceptNetstormingRequestOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1018, "UnableToAcceptNetstormingRequest"),
                $"WARNING | Test: {{message}}");
            
            BookingFinalizationFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1020, "BookingFinalizationFailure"),
                $"ERROR | BookingRequestExecutor: {{message}}");
            
            BookingFinalizationPaymentFailureOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1021, "BookingFinalizationPaymentFailure"),
                $"WARNING | BookingRequestExecutor: {{message}}");
            
            BookingFinalizationSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1022, "BookingFinalizationSuccess"),
                $"INFORMATION | BookingRequestExecutor: {{message}}");
            
            BookingFinalizationExceptionOccured = LoggerMessage.Define(LogLevel.Critical,
                new EventId(1023, "BookingFinalizationException"),
                $"CRITICAL | BookingRequestExecutor: ");
            
            BookingResponseProcessFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1030, "BookingResponseProcessFailure"),
                $"ERROR | BookingService: {{message}}");
            
            BookingResponseProcessSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1031, "BookingResponseProcessSuccess"),
                $"INFORMATION | BookingService: {{message}}");
            
            BookingResponseProcessStartedOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1032, "BookingResponseProcessStarted"),
                $"INFORMATION | BookingService: {{message}}");
            
            BookingCancelFailureOccured = LoggerMessage.Define<string>(LogLevel.Critical,
                new EventId(1040, "BookingCancelFailure"),
                $"CRITICAL | BookingService: {{message}}");
            
            BookingCancelSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1041, "BookingCancelSuccess"),
                $"INFORMATION | BookingService: {{message}}");
            
            BookingAlreadyCancelledOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1042, "BookingAlreadyCancelled"),
                $"INFORMATION | BookingService: {{message}}");
            
            BookingRegistrationSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1050, "BookingRegistrationSuccess"),
                $"INFORMATION | BookingService: {{message}}");
            
            BookingRegistrationFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1051, "BookingRegistrationFailure"),
                $"ERROR | BookingService: {{message}}");
            
            BookingByAccountSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1060, "BookingByAccountSuccess"),
                $"INFORMATION | BookingService: {{message}}");
            
            BookingByAccountFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1061, "BookingByAccountFailure"),
                $"ERROR | BookingService: {{message}}");
            
            BookingRefreshStatusSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1070, "BookingRefreshStatusSuccess"),
                $"INFORMATION | BookingService: {{message}}");
            
            BookingRefreshStatusFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1071, "BookingRefreshStatusFailure"),
                $"ERROR | BookingService: {{message}}");
            
            BookingConfirmationFailureOccured = LoggerMessage.Define<string>(LogLevel.Critical,
                new EventId(1072, "BookingConfirmationFailure"),
                $"CRITICAL | BookingChangesProcessor: {{message}}");
            
            BookingEvaluationFailureOccured = LoggerMessage.Define<string>(LogLevel.Critical,
                new EventId(1073, "BookingEvaluationFailure"),
                $"CRITICAL | BookingEvaluationService: {{message}}");
            
            AdministratorAuthorizationSuccessOccured = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1100, "AdministratorAuthorizationSuccess"),
                $"DEBUG | AdministratorPermissionsAuthorizationHandler: {{message}}");
            
            AdministratorAuthorizationFailureOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1101, "AdministratorAuthorizationFailure"),
                $"WARNING | AdministratorPermissionsAuthorizationHandler: {{message}}");
            
            AgentAuthorizationSuccessOccured = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1110, "AgentAuthorizationSuccess"),
                $"DEBUG | InAgencyPermissionAuthorizationHandler: {{message}}");
            
            AgentAuthorizationFailureOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1111, "AgentAuthorizationFailure"),
                $"WARNING | InAgencyPermissionAuthorizationHandler: {{message}}");
            
            CounterpartyAccountCreationFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1120, "CounterpartyAccountCreationFailure"),
                $"ERROR | AccountManagementService: {{message}}");
            
            CounterpartyAccountCreationSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1121, "CounterpartyAccountCreationSuccess"),
                $"INFORMATION | AccountManagementService: {{message}}");
            
            ServiceAccountAuthorizationSuccessOccured = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1125, "ServiceAccountAuthorizationSuccess"),
                $"DEBUG | InAgencyPermissionAuthorizationHandler: {{message}}");
            
            ServiceAccountAuthorizationFailureOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1126, "ServiceAccountAuthorizationFailure"),
                $"WARNING | InAgencyPermissionAuthorizationHandler: {{message}}");
            
            LocationNormalizedOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1130, "LocationNormalized"),
                $"INFORMATION | LocationNormalizer: {{message}}");
            
            MultiProviderAvailabilitySearchStartedOccured = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1140, "MultiProviderAvailabilitySearchStarted"),
                $"DEBUG | AvailabilitySearchScheduler: {{message}}");
            
            ProviderAvailabilitySearchStartedOccured = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1141, "ProviderAvailabilitySearchStarted"),
                $"DEBUG | AvailabilitySearchScheduler: {{message}}");
            
            ProviderAvailabilitySearchSuccessOccured = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1142, "ProviderAvailabilitySearchSuccess"),
                $"DEBUG | AvailabilitySearchScheduler: {{message}}");
            
            ProviderAvailabilitySearchFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1143, "ProviderAvailabilitySearchFailure"),
                $"ERROR | AvailabilitySearchScheduler: {{message}}");
            
            ProviderAvailabilitySearchExceptionOccured = LoggerMessage.Define(LogLevel.Critical,
                new EventId(1145, "ProviderAvailabilitySearchException"),
                $"CRITICAL | AvailabilitySearchScheduler: ");
            
            CounterpartyStateAuthorizationSuccessOccured = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1150, "CounterpartyStateAuthorizationSuccess"),
                $"DEBUG | MinCounterpartyStateAuthorizationHandler: {{message}}");
            
            CounterpartyStateAuthorizationFailureOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1151, "CounterpartyStateAuthorizationFailure"),
                $"WARNING | MinCounterpartyStateAuthorizationHandler: {{message}}");
            
            DefaultLanguageKeyIsMissingInFieldOfLocationsTableOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1200, "DefaultLanguageKeyIsMissingInFieldOfLocationsTable"),
                $"WARNING | LocationNormalizer: {{message}}");
            
            ConnectorClientExceptionOccured = LoggerMessage.Define(LogLevel.Critical,
                new EventId(1300, "ConnectorClientException"),
                $"CRITICAL | ConnectorClient: ");
            
            SupplierConnectorRequestErrorOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1301, "SupplierConnectorRequestError"),
                $"ERROR | SupplierConnector: {{message}}");
            
            SupplierConnectorRequestDurationOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1302, "SupplierConnectorRequestDuration"),
                $"INFORMATION | SupplierConnector: {{message}}");
            
            GetTokenForConnectorErrorOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1310, "GetTokenForConnectorError"),
                $"ERROR | ConnectorClient: {{message}}");
            
            UnauthorizedConnectorResponseOccured = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1311, "UnauthorizedConnectorResponse"),
                $"DEBUG | ConnectorClient: {{message}}");
            
            CaptureMoneyForBookingSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1400, "CaptureMoneyForBookingSuccess"),
                $"INFORMATION | BookingService: {{message}}");
            
            CaptureMoneyForBookingFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1401, "CaptureMoneyForBookingFailure"),
                $"ERROR | BookingService: {{message}}");
            
            ChargeMoneyForBookingSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1402, "ChargeMoneyForBookingSuccess"),
                $"INFORMATION | BookingService: {{message}}");
            
            ChargeMoneyForBookingFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1403, "ChargeMoneyForBookingFailure"),
                $"ERROR | BookingService: {{message}}");
            
            ProcessPaymentChangesForBookingSuccessOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(1410, "ProcessPaymentChangesForBookingSuccess"),
                $"INFORMATION | BookingService: {{message}}");
            
            ProcessPaymentChangesForBookingSkipOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(1411, "ProcessPaymentChangesForBookingSkip"),
                $"WARNING | BookingService: {{message}}");
            
            ProcessPaymentChangesForBookingFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1412, "ProcessPaymentChangesForBookingFailure"),
                $"ERROR | BookingService: {{message}}");
            
            ElasticAnalyticsEventSendErrorOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1501, "ElasticAnalyticsEventSendError"),
                $"ERROR | AnalyticsService: {{message}}");
            
            MapperClientExceptionOccured = LoggerMessage.Define(LogLevel.Error,
                new EventId(1601, "MapperClientException"),
                $"ERROR | AccommodationMapperClient: ");
            
            CounterpartyAccountAddedNotificationFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1701, "CounterpartyAccountAddedNotificationFailure"),
                $"ERROR | CounterpartyBillingNotificationService: {{message}}");
            
            AgentRegistrationNotificationFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1702, "AgentRegistrationNotificationFailure"),
                $"ERROR | InvitationService: {{message}}");
            
            ChildAgencyRegistrationNotificationFailureOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1703, "ChildAgencyRegistrationNotificationFailure"),
                $"ERROR | InvitationService: {{message}}");
            
        }
    
                
         public static void LogGeoCoderException(this ILogger logger, Exception exception)
            => GeoCoderExceptionOccured(logger, exception);
                
         public static void LogInvitationCreated(this ILogger logger, string message)
            => InvitationCreatedOccured(logger, message, null);
                
         public static void LogAgentRegistrationFailed(this ILogger logger, string message)
            => AgentRegistrationFailedOccured(logger, message, null);
                
         public static void LogAgentRegistrationSuccess(this ILogger logger, string message)
            => AgentRegistrationSuccessOccured(logger, message, null);
                
         public static void LogPayfortClientException(this ILogger logger, Exception exception)
            => PayfortClientExceptionOccured(logger, exception);
                
         public static void LogAgencyAccountCreationSuccess(this ILogger logger, string message)
            => AgencyAccountCreationSuccessOccured(logger, message, null);
                
         public static void LogAgencyAccountCreationFailed(this ILogger logger, string message)
            => AgencyAccountCreationFailedOccured(logger, message, null);
                
         public static void LogEntityLockFailed(this ILogger logger, string message)
            => EntityLockFailedOccured(logger, message, null);
                
         public static void LogPayfortError(this ILogger logger, string message)
            => PayfortErrorOccured(logger, message, null);
                
         public static void LogExternalPaymentLinkSendSuccess(this ILogger logger, string message)
            => ExternalPaymentLinkSendSuccessOccured(logger, message, null);
                
         public static void LogExternalPaymentLinkSendFailed(this ILogger logger, string message)
            => ExternalPaymentLinkSendFailedOccured(logger, message, null);
                
         public static void LogUnableGetBookingDetailsFromNetstormingXml(this ILogger logger, string message)
            => UnableGetBookingDetailsFromNetstormingXmlOccured(logger, message, null);
                
         public static void LogUnableToAcceptNetstormingRequest(this ILogger logger, string message)
            => UnableToAcceptNetstormingRequestOccured(logger, message, null);
                
         public static void LogBookingFinalizationFailure(this ILogger logger, string message)
            => BookingFinalizationFailureOccured(logger, message, null);
                
         public static void LogBookingFinalizationPaymentFailure(this ILogger logger, string message)
            => BookingFinalizationPaymentFailureOccured(logger, message, null);
                
         public static void LogBookingFinalizationSuccess(this ILogger logger, string message)
            => BookingFinalizationSuccessOccured(logger, message, null);
                
         public static void LogBookingFinalizationException(this ILogger logger, Exception exception)
            => BookingFinalizationExceptionOccured(logger, exception);
                
         public static void LogBookingResponseProcessFailure(this ILogger logger, string message)
            => BookingResponseProcessFailureOccured(logger, message, null);
                
         public static void LogBookingResponseProcessSuccess(this ILogger logger, string message)
            => BookingResponseProcessSuccessOccured(logger, message, null);
                
         public static void LogBookingResponseProcessStarted(this ILogger logger, string message)
            => BookingResponseProcessStartedOccured(logger, message, null);
                
         public static void LogBookingCancelFailure(this ILogger logger, string message)
            => BookingCancelFailureOccured(logger, message, null);
                
         public static void LogBookingCancelSuccess(this ILogger logger, string message)
            => BookingCancelSuccessOccured(logger, message, null);
                
         public static void LogBookingAlreadyCancelled(this ILogger logger, string message)
            => BookingAlreadyCancelledOccured(logger, message, null);
                
         public static void LogBookingRegistrationSuccess(this ILogger logger, string message)
            => BookingRegistrationSuccessOccured(logger, message, null);
                
         public static void LogBookingRegistrationFailure(this ILogger logger, string message)
            => BookingRegistrationFailureOccured(logger, message, null);
                
         public static void LogBookingByAccountSuccess(this ILogger logger, string message)
            => BookingByAccountSuccessOccured(logger, message, null);
                
         public static void LogBookingByAccountFailure(this ILogger logger, string message)
            => BookingByAccountFailureOccured(logger, message, null);
                
         public static void LogBookingRefreshStatusSuccess(this ILogger logger, string message)
            => BookingRefreshStatusSuccessOccured(logger, message, null);
                
         public static void LogBookingRefreshStatusFailure(this ILogger logger, string message)
            => BookingRefreshStatusFailureOccured(logger, message, null);
                
         public static void LogBookingConfirmationFailure(this ILogger logger, string message)
            => BookingConfirmationFailureOccured(logger, message, null);
                
         public static void LogBookingEvaluationFailure(this ILogger logger, string message)
            => BookingEvaluationFailureOccured(logger, message, null);
                
         public static void LogAdministratorAuthorizationSuccess(this ILogger logger, string message)
            => AdministratorAuthorizationSuccessOccured(logger, message, null);
                
         public static void LogAdministratorAuthorizationFailure(this ILogger logger, string message)
            => AdministratorAuthorizationFailureOccured(logger, message, null);
                
         public static void LogAgentAuthorizationSuccess(this ILogger logger, string message)
            => AgentAuthorizationSuccessOccured(logger, message, null);
                
         public static void LogAgentAuthorizationFailure(this ILogger logger, string message)
            => AgentAuthorizationFailureOccured(logger, message, null);
                
         public static void LogCounterpartyAccountCreationFailure(this ILogger logger, string message)
            => CounterpartyAccountCreationFailureOccured(logger, message, null);
                
         public static void LogCounterpartyAccountCreationSuccess(this ILogger logger, string message)
            => CounterpartyAccountCreationSuccessOccured(logger, message, null);
                
         public static void LogServiceAccountAuthorizationSuccess(this ILogger logger, string message)
            => ServiceAccountAuthorizationSuccessOccured(logger, message, null);
                
         public static void LogServiceAccountAuthorizationFailure(this ILogger logger, string message)
            => ServiceAccountAuthorizationFailureOccured(logger, message, null);
                
         public static void LogLocationNormalized(this ILogger logger, string message)
            => LocationNormalizedOccured(logger, message, null);
                
         public static void LogMultiProviderAvailabilitySearchStarted(this ILogger logger, string message)
            => MultiProviderAvailabilitySearchStartedOccured(logger, message, null);
                
         public static void LogProviderAvailabilitySearchStarted(this ILogger logger, string message)
            => ProviderAvailabilitySearchStartedOccured(logger, message, null);
                
         public static void LogProviderAvailabilitySearchSuccess(this ILogger logger, string message)
            => ProviderAvailabilitySearchSuccessOccured(logger, message, null);
                
         public static void LogProviderAvailabilitySearchFailure(this ILogger logger, string message)
            => ProviderAvailabilitySearchFailureOccured(logger, message, null);
                
         public static void LogProviderAvailabilitySearchException(this ILogger logger, Exception exception)
            => ProviderAvailabilitySearchExceptionOccured(logger, exception);
                
         public static void LogCounterpartyStateAuthorizationSuccess(this ILogger logger, string message)
            => CounterpartyStateAuthorizationSuccessOccured(logger, message, null);
                
         public static void LogCounterpartyStateAuthorizationFailure(this ILogger logger, string message)
            => CounterpartyStateAuthorizationFailureOccured(logger, message, null);
                
         public static void LogDefaultLanguageKeyIsMissingInFieldOfLocationsTable(this ILogger logger, string message)
            => DefaultLanguageKeyIsMissingInFieldOfLocationsTableOccured(logger, message, null);
                
         public static void LogConnectorClientException(this ILogger logger, Exception exception)
            => ConnectorClientExceptionOccured(logger, exception);
                
         public static void LogSupplierConnectorRequestError(this ILogger logger, string message)
            => SupplierConnectorRequestErrorOccured(logger, message, null);
                
         public static void LogSupplierConnectorRequestDuration(this ILogger logger, string message)
            => SupplierConnectorRequestDurationOccured(logger, message, null);
                
         public static void LogGetTokenForConnectorError(this ILogger logger, string message)
            => GetTokenForConnectorErrorOccured(logger, message, null);
                
         public static void LogUnauthorizedConnectorResponse(this ILogger logger, string message)
            => UnauthorizedConnectorResponseOccured(logger, message, null);
                
         public static void LogCaptureMoneyForBookingSuccess(this ILogger logger, string message)
            => CaptureMoneyForBookingSuccessOccured(logger, message, null);
                
         public static void LogCaptureMoneyForBookingFailure(this ILogger logger, string message)
            => CaptureMoneyForBookingFailureOccured(logger, message, null);
                
         public static void LogChargeMoneyForBookingSuccess(this ILogger logger, string message)
            => ChargeMoneyForBookingSuccessOccured(logger, message, null);
                
         public static void LogChargeMoneyForBookingFailure(this ILogger logger, string message)
            => ChargeMoneyForBookingFailureOccured(logger, message, null);
                
         public static void LogProcessPaymentChangesForBookingSuccess(this ILogger logger, string message)
            => ProcessPaymentChangesForBookingSuccessOccured(logger, message, null);
                
         public static void LogProcessPaymentChangesForBookingSkip(this ILogger logger, string message)
            => ProcessPaymentChangesForBookingSkipOccured(logger, message, null);
                
         public static void LogProcessPaymentChangesForBookingFailure(this ILogger logger, string message)
            => ProcessPaymentChangesForBookingFailureOccured(logger, message, null);
                
         public static void LogElasticAnalyticsEventSendError(this ILogger logger, string message)
            => ElasticAnalyticsEventSendErrorOccured(logger, message, null);
                
         public static void LogMapperClientException(this ILogger logger, Exception exception)
            => MapperClientExceptionOccured(logger, exception);
                
         public static void LogCounterpartyAccountAddedNotificationFailure(this ILogger logger, string message)
            => CounterpartyAccountAddedNotificationFailureOccured(logger, message, null);
                
         public static void LogAgentRegistrationNotificationFailure(this ILogger logger, string message)
            => AgentRegistrationNotificationFailureOccured(logger, message, null);
                
         public static void LogChildAgencyRegistrationNotificationFailure(this ILogger logger, string message)
            => ChildAgencyRegistrationNotificationFailureOccured(logger, message, null);
    
    
        
        private static readonly Action<ILogger, Exception> GeoCoderExceptionOccured;
        
        private static readonly Action<ILogger, string, Exception> InvitationCreatedOccured;
        
        private static readonly Action<ILogger, string, Exception> AgentRegistrationFailedOccured;
        
        private static readonly Action<ILogger, string, Exception> AgentRegistrationSuccessOccured;
        
        private static readonly Action<ILogger, Exception> PayfortClientExceptionOccured;
        
        private static readonly Action<ILogger, string, Exception> AgencyAccountCreationSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> AgencyAccountCreationFailedOccured;
        
        private static readonly Action<ILogger, string, Exception> EntityLockFailedOccured;
        
        private static readonly Action<ILogger, string, Exception> PayfortErrorOccured;
        
        private static readonly Action<ILogger, string, Exception> ExternalPaymentLinkSendSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> ExternalPaymentLinkSendFailedOccured;
        
        private static readonly Action<ILogger, string, Exception> UnableGetBookingDetailsFromNetstormingXmlOccured;
        
        private static readonly Action<ILogger, string, Exception> UnableToAcceptNetstormingRequestOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingFinalizationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingFinalizationPaymentFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingFinalizationSuccessOccured;
        
        private static readonly Action<ILogger, Exception> BookingFinalizationExceptionOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingResponseProcessFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingResponseProcessSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingResponseProcessStartedOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingCancelFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingCancelSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingAlreadyCancelledOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingRegistrationSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingRegistrationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingByAccountSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingByAccountFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingRefreshStatusSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingRefreshStatusFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingConfirmationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> BookingEvaluationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> AdministratorAuthorizationSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> AdministratorAuthorizationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> AgentAuthorizationSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> AgentAuthorizationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> CounterpartyAccountCreationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> CounterpartyAccountCreationSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> ServiceAccountAuthorizationSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> ServiceAccountAuthorizationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> LocationNormalizedOccured;
        
        private static readonly Action<ILogger, string, Exception> MultiProviderAvailabilitySearchStartedOccured;
        
        private static readonly Action<ILogger, string, Exception> ProviderAvailabilitySearchStartedOccured;
        
        private static readonly Action<ILogger, string, Exception> ProviderAvailabilitySearchSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> ProviderAvailabilitySearchFailureOccured;
        
        private static readonly Action<ILogger, Exception> ProviderAvailabilitySearchExceptionOccured;
        
        private static readonly Action<ILogger, string, Exception> CounterpartyStateAuthorizationSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> CounterpartyStateAuthorizationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> DefaultLanguageKeyIsMissingInFieldOfLocationsTableOccured;
        
        private static readonly Action<ILogger, Exception> ConnectorClientExceptionOccured;
        
        private static readonly Action<ILogger, string, Exception> SupplierConnectorRequestErrorOccured;
        
        private static readonly Action<ILogger, string, Exception> SupplierConnectorRequestDurationOccured;
        
        private static readonly Action<ILogger, string, Exception> GetTokenForConnectorErrorOccured;
        
        private static readonly Action<ILogger, string, Exception> UnauthorizedConnectorResponseOccured;
        
        private static readonly Action<ILogger, string, Exception> CaptureMoneyForBookingSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> CaptureMoneyForBookingFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> ChargeMoneyForBookingSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> ChargeMoneyForBookingFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> ProcessPaymentChangesForBookingSuccessOccured;
        
        private static readonly Action<ILogger, string, Exception> ProcessPaymentChangesForBookingSkipOccured;
        
        private static readonly Action<ILogger, string, Exception> ProcessPaymentChangesForBookingFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> ElasticAnalyticsEventSendErrorOccured;
        
        private static readonly Action<ILogger, Exception> MapperClientExceptionOccured;
        
        private static readonly Action<ILogger, string, Exception> CounterpartyAccountAddedNotificationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> AgentRegistrationNotificationFailureOccured;
        
        private static readonly Action<ILogger, string, Exception> ChildAgencyRegistrationNotificationFailureOccured;
    }
}