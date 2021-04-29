using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Documents;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments
{
    public class CreditCardPaymentConfirmationService : ICreditCardPaymentConfirmationService
    {
        public CreditCardPaymentConfirmationService(IAdministratorContext administratorContext, 
            EdoContext edoContext, 
            IBookingDocumentsService documentsService, 
            IBookingDocumentsMailingService documentsMailingService)
        {
            _administratorContext = administratorContext;
            _edoContext = edoContext;
            _documentsService = documentsService;
            _documentsMailingService = documentsMailingService;
        }

        public async Task<Result> Confirm(int bookingId)
        {
            return await GetBooking()
                .Bind(SendReceipt)
                .Bind(SaveConfirmation);


            async Task<Result<Booking>> GetBooking()
            {
                var query = from booking in _edoContext.Bookings
                    join confirmation in _edoContext.CreditCardPaymentConfirmations on booking.Id equals confirmation.BookingId into bc
                    from confirmation in bc.DefaultIfEmpty()
                    where booking.Id == bookingId
                    select new {booking, IsConfirmed = confirmation != null};

                var data = await query.SingleOrDefaultAsync();

                if (data is null)
                    return Result.Failure<Booking>($"Booking with Id {bookingId} not found");

                if (data.IsConfirmed)
                    return Result.Failure<Booking>("Payment already confirmed");

                if (data.booking.PaymentMethod != PaymentTypes.CreditCard)
                    return Result.Failure<Booking>($"Wrong payment method {data.booking.PaymentMethod}");

                return data.booking;
            }


            async Task<Result<Data.Management.Administrator>> SendReceipt(Booking booking)
            {
                var (_, isReceiptFailure, receiptInfo, receiptError) = await _documentsService.GenerateReceipt(booking);
                if (isReceiptFailure)
                    return Result.Failure<Data.Management.Administrator>(receiptError);

                var email = await _edoContext.Agents
                    .Where(a => a.Id == booking.AgentId)
                    .Select(a => a.Email)
                    .SingleOrDefaultAsync();

                var (_, isFailure, administrator, error) = await _administratorContext.GetCurrent();
                if (isFailure)
                    return Result.Failure<Data.Management.Administrator>(error);

                await _documentsMailingService.SendReceiptToCustomer(receiptInfo, email, new Models.Users.ApiCaller(administrator.Id, ApiCallerTypes.Admin));
                
                return administrator;
            }


            async Task<Result> SaveConfirmation(Data.Management.Administrator administrator)
            {
                await _edoContext.CreditCardPaymentConfirmations.AddAsync(new CreditCardPaymentConfirmation
                {
                    BookingId = bookingId,
                    AdministratorId = administrator.Id,
                    ConfirmedAt = DateTime.UtcNow
                });
                await _edoContext.SaveChangesAsync();
                return Result.Success();
            }
        }


        private readonly IAdministratorContext _administratorContext;
        private readonly EdoContext _edoContext;
        private readonly IBookingDocumentsService _documentsService;
        private readonly IBookingDocumentsMailingService _documentsMailingService;
    }
}