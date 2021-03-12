using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Payments.NGenius
{
    public class NGeniusPaymentService
    {
        public NGeniusPaymentService(IHttpClientFactory httpClientFactory, 
            IBookingRecordManager bookingRecordManager,
            EdoContext context)
        {
            _httpClientFactory = httpClientFactory;
            _bookingRecordManager = bookingRecordManager;
            _context = context;
        }


        public async Task<Result> Authorize(string referenceCode, PaymentInformation paymentInformation)
        {
            var (isSuccess, _, state) = await _bookingRecordManager.Get(referenceCode)
                .Bind(b => CreateOrder(b, OrderTypes.Auth, paymentInformation));
            
            // TODO: update booking payment status

            return isSuccess && state == StateTypes.Authorized 
                ? Result.Success() 
                : Result.Failure("Payment authorization failed");
        }


        public async Task<Result> Pay(string referenceCode, PaymentInformation paymentInformation)
        {
            var (isSuccess, _, state) = await _bookingRecordManager.Get(referenceCode)
                .Bind(b => CreateOrder(b, OrderTypes.Sale, paymentInformation));

            // TODO: update booking payment status
            
            return isSuccess && state == StateTypes.Captured 
                ? Result.Success() 
                : Result.Failure("Payment failed");
        }


        public async Task<Result> Capture(Guid paymentId, string referenceCode, MoneyAmount amount)
        {
            var data = new Amount
            {
                CurrencyCode = amount.Currency.ToString(),
                Value = amount.Amount
            };
            
            // TODO: add authorization
            var endpoint = $"{Endpoint}/{_outletId}/orders/{referenceCode}/{paymentId}/captures";
            using var client = _httpClientFactory.CreateClient("");
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
            });
            
            response.EnsureSuccessStatusCode();
            
            // TODO: get and store captureId

            return Result.Success();
        }


        public async Task<Result> Void(Guid paymentId, string referenceCode)
        {
            var endpoint = $"{Endpoint}/{_outletId}/orders/{referenceCode}/{paymentId}/cancel";
            using var client = _httpClientFactory.CreateClient("");
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, endpoint));
            
            response.EnsureSuccessStatusCode();

            return Result.Success();
        }
        
        
        public async Task<Result> Refund(Guid paymentId, Guid captureId, string referenceCode)
        {
            var endpoint = $"{Endpoint}/{_outletId}/orders/{referenceCode}/{paymentId}/captures/{captureId}/refund";
            using var client = _httpClientFactory.CreateClient("");
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, endpoint));
            
            response.EnsureSuccessStatusCode();

            return Result.Success();
        }
        
        
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


            async Task<Result<Guid>> MakeOrder(Agent agent)
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
                    MerchantOrderReference = booking.ReferenceCode,
                    MerchantAttributes = new MerchantAttributes
                    {
                        Skip3DS = true
                    }
                };

                // TODO: add authorization
                var endpoint = $"{Endpoint}/{_outletId}/orders";
                using var client = _httpClientFactory.CreateClient("");
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                });

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(json);
                var paymentId =  document.RootElement
                    .GetProperty("_embedded")
                    .GetProperty("payments")
                    .EnumerateArray()
                    .Take(1)
                    .FirstOrDefault()
                    .GetProperty("_id")
                    .GetString()
                    ?.Split(":")
                    .LastOrDefault();

                if (string.IsNullOrEmpty(paymentId) || Guid.TryParse(paymentId, out var _))
                    return Result.Failure<Guid>("Failed to get payment id");

                return new Guid(paymentId);
            }
            
            
            async Task<Result<string>> AddPaymentInformation(Guid paymentId)
            {
                // TODO: add authorization
                var endpoint = $"{Endpoint}/{_outletId}/orders/{booking.ReferenceCode}/{paymentId}/card";
                using var client = _httpClientFactory.CreateClient("");
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, endpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(paymentInformation), Encoding.UTF8, "application/json")
                });

                response.EnsureSuccessStatusCode();
            
                var json = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(json);
                return document.RootElement
                    .GetProperty("state")
                    .GetString();
            }
        }


        private readonly Guid _outletId = Guid.NewGuid(); // TODO: change to outlet id from NGenius
        private const string Endpoint = "https://api-gateway.sandbox.ngenius-payments.com/transactions/outlets/";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBookingRecordManager _bookingRecordManager;
        private readonly EdoContext _context;
    }
}