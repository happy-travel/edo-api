using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.DataFormatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HappyTravel.Edo.Notifications.Enums;
using HappyTravel.Edo.Api.NotificationCenter.Services;
using HappyTravel.Edo.Api.Models.Agents;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class CounterpartyBillingNotificationService : ICounterpartyBillingNotificationService
    {
        public CounterpartyBillingNotificationService(INotificationService notificationService,
            Services.Agents.IAgentService agentService,
            ICounterpartyService counterpartyService,
            ILogger<CounterpartyBillingNotificationService> logger,
            IOptions<CounterpartyBillingNotificationServiceOptions> options)
        {
            _notificationService = notificationService;
            _agentService = agentService;
            _counterpartyService = counterpartyService;
            _logger = logger;
            _options = options.Value;
        }


        public Task NotifyAdded(int counterpartyId, PaymentData paymentData)
        {
            return GetEmailAndAgent(counterpartyId)
                .Bind(receiver => SendNotification(receiver, paymentData, NotificationTypes.CounterpartyAccountBalanceReplenished))
                .OnFailure(LogNotificationFailure);


            void LogNotificationFailure(string error) => _logger.LogCounterpartyAccountAddedNotificationFailure(counterpartyId, error);
        }


        public Task NotifySubtracted(int counterpartyId, PaymentData paymentData)
        {
            return GetEmailAndAgent(counterpartyId)
                .Bind(receiver => SendNotification(receiver, paymentData, NotificationTypes.CounterpartyAccountBalanceSubtracted))
                .OnFailure(LogNotificationFailure);


            void LogNotificationFailure(string error) => _logger.LogCounterpartyAccountSubtractedNotificationFailure(counterpartyId, error);
        }


        public Task NotifyIncreasedManually(int counterpartyId, PaymentData paymentData)
        {
            return GetEmailAndAgent(counterpartyId)
                .Bind(receiver => SendNotification(receiver, paymentData, NotificationTypes.CounterpartyAccountBalanceIncreasedManually))
                .OnFailure(LogNotificationFailure);


            void LogNotificationFailure(string error) => _logger.LogCounterpartyAccountIncreasedManuallyNotificationFailure(counterpartyId, error);
        }


        public Task NotifyDecreasedManually(int counterpartyId, PaymentData paymentData)
        {
            return GetEmailAndAgent(counterpartyId)
                .Bind(receiver => SendNotification(receiver, paymentData, NotificationTypes.CounterpartyAccountBalanceDecreasedManually))
                .OnFailure(LogNotificationFailure);


            void LogNotificationFailure(string error) => _logger.LogCounterpartyAccountDecreasedManuallyNotificationFailure(counterpartyId, error);
        }


        private async Task<Result<(string, SlimAgentContext)>> GetEmailAndAgent(int counterpartyId)
        {
            var rootAgency = await _counterpartyService.GetRootAgency(counterpartyId);

            var (_, isFailure, agent, error) = await _agentService.GetMasterAgent(rootAgency.Id);
            if (isFailure)
                return Result.Failure<(string, SlimAgentContext)>(error);

            var slimAgent = new SlimAgentContext(agent.Id, rootAgency.Id);

            var email = string.IsNullOrWhiteSpace(rootAgency.BillingEmail)
                ? agent.Email
                : rootAgency.BillingEmail;

            return (email, slimAgent);
        }


        private async Task<Result> SendNotification((string, SlimAgentContext) receiver, PaymentData paymentData, NotificationTypes notificationType)
        {
            var payload = new CounterpartyAccountAddedNotificationData
            {
                Amount = MoneyFormatter.ToCurrencyString(paymentData.Amount, paymentData.Currency)
            };
            var (email, agent) = receiver;
            string templateId = notificationType switch
            {
                NotificationTypes.CounterpartyAccountBalanceReplenished => _options.CounterpartyAccountAddedTemplateId,
                NotificationTypes.CounterpartyAccountBalanceSubtracted => _options.CounterpartyAccountSubtractedTemplateId,
                NotificationTypes.CounterpartyAccountBalanceIncreasedManually => _options.CounterpartyAccountIncreasedManuallyTemplateId,
                NotificationTypes.CounterpartyAccountBalanceDecreasedManually => _options.CounterpartyAccountDecreasedManuallyTemplateId,
                _ => throw new NotImplementedException()
            };

            return await _notificationService.Send(agent: agent,
                messageData: payload,
                notificationType: notificationType,
                email: email,
                templateId: templateId);
        }


        private readonly INotificationService _notificationService;
        private readonly Services.Agents.IAgentService _agentService;
        private readonly ICounterpartyService _counterpartyService;
        private readonly ILogger<CounterpartyBillingNotificationService> _logger;
        private readonly CounterpartyBillingNotificationServiceOptions _options;
    }
}