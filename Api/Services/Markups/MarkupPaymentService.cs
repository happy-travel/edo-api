using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class MarkupPaymentService : IMarkupPaymentService
    {
        public MarkupPaymentService(EdoContext context, IMarkupPaymentAuditLogService logService, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _logService = logService;
            _dateTimeProvider = dateTimeProvider;
        }


        public async Task<Result> Pay(string referenceCode)
        {
            var bookingMarkupPayment = await GetPaymentData();

            if (bookingMarkupPayment.Equals(default))
                return Result.Success();

            return await AppendMoney()
                .Tap(WriteLog);


            Task<BookingMarkupPayment> GetPaymentData()
            {
                var query = from markupPolicy in _context.MarkupPolicies
                    join bookingMarkup in _context.BookingMarkups on markupPolicy.Id equals bookingMarkup.PolicyId
                    join agencyAccount in _context.AgencyAccounts on bookingMarkup.AgencyId equals agencyAccount.AgencyId
                    where bookingMarkup.ReferenceCode == referenceCode &&
                        markupPolicy.ScopeType == MarkupPolicyScopeType.Agent &&
                        bookingMarkup.PayedAt == null
                    select new BookingMarkupPayment(bookingMarkup.Id,
                        referenceCode,
                        new MoneyAmount(bookingMarkup.Amount, bookingMarkup.Currency), agencyAccount.Id);

                return query.SingleOrDefaultAsync();
            }


            async Task<Result> AppendMoney()
            {
                var agencyAccount = await _context.AgencyAccounts
                    .SingleOrDefaultAsync(a => a.Id == bookingMarkupPayment.AgencyAccountId);

                if (agencyAccount is null)
                    return Result.Failure($"Agency account with id {bookingMarkupPayment.AgencyAccountId} not found");

                var bookingMarkup = await _context.BookingMarkups
                    .SingleOrDefaultAsync(b => b.Id == bookingMarkupPayment.BookingMarkupId);

                if (bookingMarkup is null)
                    return Result.Failure($"Booking markup with id {bookingMarkupPayment.BookingMarkupId} not found");

                agencyAccount.Balance += bookingMarkupPayment.MoneyAmount.Amount;
                bookingMarkup.PayedAt = _dateTimeProvider.UtcNow();
                await _context.SaveChangesAsync();
                return Result.Success();
            }


            Task WriteLog()
            {
                return _logService.Write(new MarkupPaymentLog
                {
                    AgencyAccountId = bookingMarkupPayment.AgencyAccountId,
                    CreatedAt = _dateTimeProvider.UtcNow(),
                    Amount = bookingMarkupPayment.MoneyAmount.Amount,
                    Currency = bookingMarkupPayment.MoneyAmount.Currency,
                    BookingMarkupId = bookingMarkupPayment.BookingMarkupId,
                    ReferenceCode = bookingMarkupPayment.ReferenceCode
                });
            }
        }


        private readonly EdoContext _context;
        private readonly IMarkupPaymentAuditLogService _logService;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}