using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Markup;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class PaymentMarkupService : IPaymentMarkupService
    {
        public PaymentMarkupService(EdoContext edoContext, IDateTimeProvider dateTime, ICurrencyConverterService converter)
        {
            _edoContext = edoContext;
            _dateTime = dateTime;
            _converter = converter;
        }


        public Task<Result> Pay(int bookingId)
            => GetMarkupAndPay(bookingId, false);


        public Task<Result> Refund(int bookingId)
            => GetMarkupAndPay(bookingId, true);


        private async Task<Result> GetMarkupAndPay(int bookingId, bool isWriteOff)
        {
            return await GetData(bookingId)
                .Bind(GetValue)
                .Bind(ChangeBalance)
                .Tap(WriteLog);

            Task<Result<PaymentMarkupDataWithValue>> GetValue(PaymentMarkupData data)
                => GetMarkupValue(data, isWriteOff);
        }


        private async Task<Result<PaymentMarkupDataWithValue>> GetMarkupValue(PaymentMarkupData data, bool isWriteOff)
        {
            // TODO: get markup value
            const decimal markup = 0;

            var (_, isFailure, value, error) = await _converter.Convert(data.SourceCurrency, data.TargetCurrency, markup);
            value = isWriteOff ? -value : value;

            return isFailure
                ? Result.Failure<PaymentMarkupDataWithValue>(error)
                : new PaymentMarkupDataWithValue(data, value);
        }


        private async Task<Result<PaymentMarkupData>> GetData(int bookingId)
        {
            var query = from booking in _edoContext.Bookings
                join agencyAccount in _edoContext.AgencyAccounts on booking.AgencyId equals agencyAccount.AgencyId
                where booking.Id == bookingId
                select new PaymentMarkupData(booking.Id, agencyAccount.Id, booking.Currency, agencyAccount.Currency);

            var result = await query.SingleOrDefaultAsync();

            return result.Equals(default)
                ? Result.Failure<PaymentMarkupData>($"Cannot find agency account by booking id {bookingId}")
                : result;
        }


        private async Task WriteLog(PaymentMarkupDataWithValue data)
        {
            var logRecord = new PaymentMarkupLog
            {
                AccountId = data.AgencyAccountId,
                BookingId = data.BookingId,
                Currency = data.Currency,
                Amount = data.Value,
                CreatedAt = _dateTime.UtcNow()
            };

            _edoContext.PaymentMarkupLogs.Add(logRecord);
            await _edoContext.SaveChangesAsync();
        }


        private async Task<Result<PaymentMarkupDataWithValue>> ChangeBalance(PaymentMarkupDataWithValue data)
        {
            await using var transaction = await _edoContext.Database.BeginTransactionAsync();

            var agencyAccount = await _edoContext.AgencyAccounts
                .SingleOrDefaultAsync(a => a.Id == data.AgencyAccountId);

            if(agencyAccount is null)
                return Result.Failure<PaymentMarkupDataWithValue>($"Cannot find agency account by id {data.AgencyAccountId}");

            agencyAccount.Balance += data.Value;
            await _edoContext.SaveChangesAsync();

            await transaction.CommitAsync();
            return data;
        }


        private readonly EdoContext _edoContext;
        private readonly IDateTimeProvider _dateTime;
        private readonly ICurrencyConverterService _converter;
    }
}