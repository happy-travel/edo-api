using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Payments.NGenius
{
    public class NGeniusPaymentService
    {
        public NGeniusPaymentService(IHttpClientFactory httpClientFactory, 
            IBookingRecordManager bookingRecordManager,
            EdoContext context,
            NGeniusHttpClient client)
        {
            _httpClientFactory = httpClientFactory;
            _bookingRecordManager = bookingRecordManager;
            _context = context;
            _client = client;
        }


        public async Task<Result<string>> Authorize(string referenceCode, PaymentInformation paymentInformation)
        {
            var (isSuccess, _, state) = await _bookingRecordManager.Get(referenceCode)
                .Bind(b => CreateOrder(b, OrderTypes.Auth, paymentInformation));
            
            // TODO: update booking payment status

            return isSuccess
                ? state 
                : Result.Failure<string>("Payment authorization failed");
        }


        public async Task<Result> Pay(string referenceCode, PaymentInformation paymentInformation)
        {
            var (isSuccess, _, state) = await _bookingRecordManager.Get(referenceCode)
                .Bind(b => CreateOrder(b, OrderTypes.Sale, paymentInformation));

            // TODO: update booking payment status
            
            return isSuccess
                ? state 
                : Result.Failure<string>("Payment failed");
        }


        public async Task<Result> Capture(Guid paymentId, string referenceCode, MoneyAmount amount)
        {
            var data = new Amount
            {
                CurrencyCode = amount.Currency.ToString(),
                Value = amount.Amount
            };

            var captureId = await _client.Capture(paymentId, referenceCode, data);
            
            // TODO: store captureId
            
            return Result.Success();
        }


        public Task<Result> Void(Guid paymentId, string referenceCode) 
            => _client.Void(paymentId, referenceCode);


        public Task<Result> Refund(Guid paymentId, Guid captureId, string referenceCode) 
            => _client.Refund(paymentId, captureId, referenceCode);
        
        
        private async Task<Result<string>> CreateOrder(Booking booking, string actionType, PaymentInformation paymentInformation)
        {
            return await GetAgent(booking.AgentId)
                .Bind(MakeOrder)
                .Bind(AddPaymentInformation);


            async Task<Result<Agent>> GetAgent(int id)
            {
                var agent = await _context.Agents
                    .SingleOrDefaultAsync(a => a.Id == booking.AgentId);

                return agent ?? Result.Failure<Agent>("Agent not found");
            }


            Task<Result<Guid>> MakeOrder(Agent agent)
            {
                var data = new OrderRequest
                {
                    Action = actionType,
                    Amount = new Amount
                    {
                        CurrencyCode = booking.Currency.ToString(),
                        Value = booking.TotalPrice
                    },
                    EmailAddress = "",
                    BillingAddress = new BillingAddress
                    {
                        FirstName = agent.FirstName,
                        LastName = agent.LastName
                    },
                    Language = booking.LanguageCode,
                    MerchantOrderReference = booking.ReferenceCode
                };

                return _client.CreateOrder(data);
            }


            Task<Result<string>> AddPaymentInformation(Guid paymentId)
                => _client.AddPaymentInformation(booking.ReferenceCode, paymentId, paymentInformation);
        }


        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBookingRecordManager _bookingRecordManager;
        private readonly EdoContext _context;
        private readonly NGeniusHttpClient _client;
    }
}