using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Data;
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
                .Bind(b => CreateOrder(b, OrderTypes.Auth))
                .Bind(p => AddPaymentInformation(p, referenceCode, paymentInformation));

            return isSuccess && state == StateTypes.Authorized 
                ? Result.Success() 
                : Result.Failure("Payment authorization failed");
        }


        public async Task<Result> Pay(string referenceCode, PaymentInformation paymentInformation)
        {
            var (isSuccess, _, state) = await _bookingRecordManager.Get(referenceCode)
                .Bind(b => CreateOrder(b, OrderTypes.Sale))
                .Bind(p => AddPaymentInformation(p, referenceCode, paymentInformation));
            
            return isSuccess && state == StateTypes.Captured 
                ? Result.Success() 
                : Result.Failure("Payment authorization failed");
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
        
        
        private async Task<Result<Guid>> CreateOrder(Booking booking, string actionType)
        {
            var billingAddress = await _context.Agents
                .Where(a => a.Id == booking.AgentId)
                .Select(a => new {a.FirstName, a.LastName})
                .SingleOrDefaultAsync();

            if (billingAddress is null)
                return Result.Failure<Guid>($"Agent not found");
            
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
                    FirstName = billingAddress.FirstName,
                    LastName = billingAddress.LastName
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
            var paymentId =  new Guid(document.RootElement
                .GetProperty("_embedded")
                .GetProperty("payments")
                .EnumerateArray()
                .Take(1)
                .FirstOrDefault()
                .GetProperty("_id")
                .GetString()
                ?.Split(":")
                .LastOrDefault() ?? string.Empty);
            
            // TODO: store payment id
            
            return paymentId;
        }


        private async Task<Result<string>> AddPaymentInformation(Guid paymentId, string referenceCode, PaymentInformation paymentInformation)
        {
            // TODO: add authorization
            var endpoint = $"{Endpoint}/{_outletId}/orders/{referenceCode}/{paymentId}/card";
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


        private readonly Guid _outletId = Guid.NewGuid(); // TODO: change to outlet id from NGenius
        private const string Endpoint = "https://api-gateway.sandbox.ngenius-payments.com/transactions/outlets/";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBookingRecordManager _bookingRecordManager;
        private readonly EdoContext _context;
    }
}