using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.BookingExecution
{
    public class BookingRegistrationService : IBookingRegistrationService
    {
        public BookingRegistrationService(EdoContext context,
            ITagProcessor tagProcessor,
            IDateTimeProvider dateTimeProvider,
            IAppliedBookingMarkupRecordsManager appliedBookingMarkupRecordsManager,
            IBookingChangeLogService changeLogService,
            ISupplierOrderService supplierOrderService)
        {
            _context = context;
            _tagProcessor = tagProcessor;
            _dateTimeProvider = dateTimeProvider;
            _appliedBookingMarkupRecordsManager = appliedBookingMarkupRecordsManager;
            _changeLogService = changeLogService;
            _supplierOrderService = supplierOrderService;
        }
        
        
        public async Task<Booking> Register(AccommodationBookingRequest bookingRequest,
            BookingAvailabilityInfo availabilityInfo, PaymentMethods paymentMethod, AgentContext agentContext, string languageCode)
        {
            var (_, _, booking, _) = await Result.Success()
                .Map(GetTags)
                .Map(Create)
                .Tap(LogBookingStatus)
                .Tap(SaveMarkups)
                .Tap(CreateSupplierOrder);

            return booking;


            async Task<(string itn, string referenceCode)> GetTags()
            {
                string itn;
                if (string.IsNullOrWhiteSpace(bookingRequest.ItineraryNumber))
                {
                    itn = await _tagProcessor.GenerateItn();
                }
                else
                {
                    // User can send reference code instead of itn
                    if (!_tagProcessor.TryGetItnFromReferenceCode(bookingRequest.ItineraryNumber, out itn))
                        itn = bookingRequest.ItineraryNumber;

                    if (!await AreExistBookingsForItn(itn, agentContext.AgentId))
                        itn = await _tagProcessor.GenerateItn();
                }

                var referenceCode = await _tagProcessor.GenerateReferenceCode(
                    ServiceTypes.HTL,
                    availabilityInfo.CountryCode,
                    itn);

                return (itn, referenceCode);
            }


            async Task<Booking> Create((string itn, string referenceCode) tags)
            {
                var createdBooking = BookingRegistrationService.Create(
                    _dateTimeProvider.UtcNow(),
                    agentContext,
                    tags.itn,
                    tags.referenceCode,
                    availabilityInfo,
                    paymentMethod,
                    bookingRequest,
                    languageCode,
                    availabilityInfo.Supplier,
                    availabilityInfo.RoomContractSet.Deadline.Date,
                    availabilityInfo.CheckInDate,
                    availabilityInfo.CheckOutDate,
                    availabilityInfo.HtId,
                    availabilityInfo.RoomContractSet.Tags,
                    availabilityInfo.IsDirectContract);

                _context.Bookings.Add(createdBooking);
                await _context.SaveChangesAsync();
                _context.Entry(createdBooking).State = EntityState.Detached;

                return createdBooking;
            }


            Task LogBookingStatus(Booking booking)
            {
                var changeReason = new BookingChangeReason
                {
                    Event = BookingChangeEvents.Create,
                    Source = BookingChangeSources.System
                };
                return _changeLogService.Write(booking, BookingStatuses.Created, booking.Created, 
                    agentContext.ToUserInfo(), changeReason);
            }


            Task SaveMarkups(Booking booking) 
                => _appliedBookingMarkupRecordsManager.Create(booking.ReferenceCode, availabilityInfo.AppliedMarkups);


            Task CreateSupplierOrder(Booking booking) 
                => _supplierOrderService.Add(booking.ReferenceCode, ServiceTypes.HTL, availabilityInfo.ConvertedSupplierPrice, availabilityInfo.OriginalSupplierPrice, booking.Supplier);
        }


        private static Booking Create(DateTime created, AgentContext agentContext, string itineraryNumber,
            string referenceCode, BookingAvailabilityInfo availabilityInfo, PaymentMethods paymentMethod,
            in AccommodationBookingRequest bookingRequest, string languageCode, Suppliers supplier,
            DateTime? deadlineDate, DateTime checkInDate, DateTime checkOutDate, string htId, List<string> tags, bool isDirectContract)
        {
            var booking = new Booking
            {
                Created = created,
                ItineraryNumber = itineraryNumber,
                ReferenceCode = referenceCode,
                Status = BookingStatuses.Created,
                PaymentMethod = paymentMethod,
                LanguageCode = languageCode,
                Supplier = supplier,
                PaymentStatus = BookingPaymentStatuses.NotPaid,
                DeadlineDate = deadlineDate,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                HtId = htId,
                Tags = tags,
                IsDirectContract = isDirectContract,
                CancellationPolicies = availabilityInfo.RoomContractSet.Deadline.Policies
            };
            
            AddRequestInfo(bookingRequest);
            AddServiceDetails();
            AddAgentInfo();
            AddRooms(availabilityInfo.RoomContractSet.Rooms, bookingRequest.RoomDetails);

            return booking;


            void AddRequestInfo(in AccommodationBookingRequest bookingRequestInternal)
            {
                booking.Nationality = bookingRequestInternal.Nationality;
                booking.Residency = bookingRequestInternal.Residency;
                booking.MainPassengerName = bookingRequestInternal.MainPassengerName.Trim();
            }

            void AddServiceDetails()
            {
                var rate = availabilityInfo.RoomContractSet.Rate;
                booking.TotalPrice = rate.FinalPrice[paymentMethod].Amount;
                booking.Currency = rate.FinalPrice[paymentMethod].Currency;
                booking.Location = new AccommodationLocation(availabilityInfo.CountryName,
                    availabilityInfo.LocalityName,
                    availabilityInfo.ZoneName,
                    availabilityInfo.Address,
                    availabilityInfo.Coordinates);

                booking.AccommodationId = availabilityInfo.AccommodationId;
                booking.AccommodationName = availabilityInfo.AccommodationName;
            }

            void AddAgentInfo()
            {
                booking.AgentId = agentContext.AgentId;
                booking.AgencyId = agentContext.AgencyId;
                booking.CounterpartyId = agentContext.CounterpartyId;
            }
            
            void AddRooms(List<RoomContract> roomContracts, List<BookingRoomDetails> bookingRequestRoomDetails)
            {
                booking.Rooms = roomContracts
                    .Select((r, number) =>
                        new BookedRoom(r.Type,
                            r.IsExtraBedNeeded,
                            r.Rate.FinalPrice[booking.PaymentMethod], 
                            r.BoardBasis,
                            r.MealPlan,
                            r.Deadline.Date,
                            r.ContractDescription,
                            r.Remarks,
                            r.Deadline,
                            GetCorrespondingPassengers(number),
                            r.IsAdvancePurchaseRate,
                            string.Empty))
                    .ToList();


                List<Passenger> GetCorrespondingPassengers(int number)
                    => bookingRequestRoomDetails[number].Passengers
                        .Select(p => new Passenger(p.Title, p.LastName.Trim(), p.FirstName.Trim(), p.IsLeader, p.Age))
                        .ToList();
            }
        }


        // TODO: Replace method when will be added other services 
        private Task<bool> AreExistBookingsForItn(string itn, int agentId)
            => _context.Bookings.Where(b => b.AgentId == agentId && b.ItineraryNumber == itn).AnyAsync();
        
        
        private readonly EdoContext _context;
        private readonly ITagProcessor _tagProcessor;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAppliedBookingMarkupRecordsManager _appliedBookingMarkupRecordsManager;
        private readonly IBookingChangeLogService _changeLogService;
        private readonly ISupplierOrderService _supplierOrderService;
    }
}