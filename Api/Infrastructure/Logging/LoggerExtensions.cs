using System;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Services.Payments.Payfort;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Infrastructure.Logging
{
    internal static class LoggerExtensions
    {
        static LoggerExtensions()
        {
            DataProviderClientExceptionOccurred = LoggerMessage.Define(LogLevel.Critical,
                new EventId((int) LoggerEvents.DataProviderClientException, LoggerEvents.DataProviderClientException.ToString()),
                $"CRITICAL | {nameof(DataProviderClient)}: ");
            DataProviderRequestErrorOccurred = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId((int) LoggerEvents.DataProviderRequestError, LoggerEvents.DataProviderRequestError.ToString()),
                $"ERROR | {nameof(DataProviderClient)}: {{message}}");
            GeoCoderExceptionOccurred = LoggerMessage.Define(LogLevel.Error,
                new EventId((int) LoggerEvents.GeocoderException, LoggerEvents.GeocoderException.ToString()),
                $"EXCEPTION | {nameof(GoogleGeoCoder)}: ");
            InvitationCreatedEventOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId((int) LoggerEvents.InvitationCreatedInformation, LoggerEvents.InvitationCreatedInformation.ToString()),
                $"INFORMATION | {nameof(CustomerInvitationService)}: {{message}}");
            CustomerRegistrationFailedEventOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId((int) LoggerEvents.CustomerRegistrationFailed, LoggerEvents.CustomerRegistrationFailed.ToString()),
                $"ERROR | {nameof(CustomerRegistrationService)}: {{message}}");
            CustomerRegistrationSuccessEventOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId((int) LoggerEvents.CustomerRegistrationSuccess, LoggerEvents.CustomerRegistrationSuccess.ToString()),
                $"INFORMATION | {nameof(CustomerRegistrationService)}: {{message}}");
            PaymentAccountCreationFailedEventOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId((int) LoggerEvents.PaymentAccountCreationFailed, LoggerEvents.PaymentAccountCreationFailed.ToString()),
                $"ERROR | {nameof(AccountManagementService)}: {{message}}");
            PaymentAccountCreatedSuccessEventOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId((int) LoggerEvents.PaymentAccountCreationSuccess, LoggerEvents.PaymentAccountCreationSuccess.ToString()),
                $"INFORMATION | {nameof(AccountManagementService)}: {{message}}");

            EntityLockFailedEventOccured = LoggerMessage.Define<string>(LogLevel.Critical,
                new EventId((int) LoggerEvents.EntityLockFailed, LoggerEvents.EntityLockFailed.ToString()),
                $"ERROR | {nameof(EntityLocker)}: {{message}}");
            PayfortClientExceptionOccurred = LoggerMessage.Define(LogLevel.Critical,
                new EventId((int) LoggerEvents.PayfortClientException, LoggerEvents.PayfortClientException.ToString()),
                $"CRITICAL | {nameof(PayfortService)}: ");
            PayfortErrorOccurred = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId((int) LoggerEvents.PayfortError, LoggerEvents.PayfortError.ToString()),
                $"ERROR | {nameof(PayfortService)}: {{message}}");

            ExternalPaymentLinkSendSuccessEventOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId((int) LoggerEvents.ExternalPaymentLinkSendSuccess, LoggerEvents.ExternalPaymentLinkSendSuccess.ToString()),
                $"INFORMATION | {nameof(PaymentLinkService)}: {{message}}");

            ExternalPaymentLinkSendFailedEventOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId((int) LoggerEvents.ExternalPaymentLinkSendFailed, LoggerEvents.ExternalPaymentLinkSendFailed.ToString()),
                $"ERROR | {nameof(PaymentLinkService)}: {{message}}");
        }


        internal static void LogDataProviderClientException(this ILogger logger, Exception exception) => DataProviderClientExceptionOccurred(logger, exception);
        
        internal static void LogDataProviderRequestError(this ILogger logger, string message) => DataProviderRequestErrorOccurred(logger, message, null);

        internal static void LogGeocoderException(this ILogger logger, Exception exception) => GeoCoderExceptionOccurred(logger, exception);


        internal static void LogInvitationCreatedInformation(this ILogger logger, string message) => InvitationCreatedEventOccured(logger, message, null);


        internal static void LogCustomerRegistrationFailed(this ILogger logger, string message)
            => CustomerRegistrationFailedEventOccured(logger, message, null);


        internal static void LogCustomerRegistrationSuccess(this ILogger logger, string message)
            => CustomerRegistrationSuccessEventOccured(logger, message, null);


        internal static void LogPaymentAccountCreationFailed(this ILogger logger, string message)
            => PaymentAccountCreationFailedEventOccured(logger, message, null);


        internal static void LogPaymentAccountCreationSuccess(this ILogger logger, string message)
            => PaymentAccountCreatedSuccessEventOccured(logger, message, null);


        internal static void LogEntityLockFailed(this ILogger logger, string message) => EntityLockFailedEventOccured(logger, message, null);


        internal static void LogPayfortClientException(this ILogger logger, Exception exception) => PayfortClientExceptionOccurred(logger, exception);

        internal static void LogPayfortError(this ILogger logger, string message) => PayfortErrorOccurred(logger, message, null);


        internal static void LogExternalPaymentLinkSendSuccess(this ILogger logger, string message)
            => ExternalPaymentLinkSendSuccessEventOccured(logger, message, null);


        internal static void LogExternalPaymentLinkSendFailed(this ILogger logger, string message)
            => ExternalPaymentLinkSendFailedEventOccured(logger, message, null);


        private static readonly Action<ILogger, Exception> DataProviderClientExceptionOccurred;
        private static readonly Action<ILogger, string, Exception> DataProviderRequestErrorOccurred;
        private static readonly Action<ILogger, Exception> GeoCoderExceptionOccurred;
        private static readonly Action<ILogger, Exception> PayfortClientExceptionOccurred;
        private static readonly Action<ILogger, string, Exception> PayfortErrorOccurred;
        private static readonly Action<ILogger, string, Exception> InvitationCreatedEventOccured;
        private static readonly Action<ILogger, string, Exception> CustomerRegistrationFailedEventOccured;
        private static readonly Action<ILogger, string, Exception> CustomerRegistrationSuccessEventOccured;
        private static readonly Action<ILogger, string, Exception> PaymentAccountCreationFailedEventOccured;
        private static readonly Action<ILogger, string, Exception> PaymentAccountCreatedSuccessEventOccured;
        private static readonly Action<ILogger, string, Exception> EntityLockFailedEventOccured;
        private static readonly Action<ILogger, string, Exception> ExternalPaymentLinkSendSuccessEventOccured;
        private static readonly Action<ILogger, string, Exception> ExternalPaymentLinkSendFailedEventOccured;
    }
}