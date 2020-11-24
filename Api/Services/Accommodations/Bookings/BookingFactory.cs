using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Models;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public static class BookingFactory
    {
        public static Booking Create(
            DateTime created,
            AgentContext agentContext,
            string itineraryNumber,
            string referenceCode,
            BookingAvailabilityInfo availabilityInfo,
            PaymentMethods paymentMethod,
            in AccommodationBookingRequest bookingRequest,
            string languageCode,
            Suppliers supplier,
            DateTime? deadlineDate, 
            DateTime checkInDate,
            DateTime checkOutDate)
        {
            var booking = new Booking
            {
                Created = created,
                ItineraryNumber = itineraryNumber,
                ReferenceCode = referenceCode,
                Status = BookingStatuses.InternalProcessing,
                PaymentMethod = paymentMethod,
                LanguageCode = languageCode,
                Supplier = supplier,
                PaymentStatus = BookingPaymentStatuses.NotPaid,
                DeadlineDate = deadlineDate,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate
            };
            
            AddRequestInfo(bookingRequest);
            AddServiceDetails();
            AddAgentInfo();
            AddRooms(availabilityInfo.RoomContractSet.RoomContracts, bookingRequest.RoomDetails);

            return booking;


            void AddRequestInfo(in AccommodationBookingRequest bookingRequestInternal)
            {
                booking.Nationality = bookingRequestInternal.Nationality;
                booking.Residency = bookingRequestInternal.Residency;
                booking.MainPassengerName = bookingRequestInternal.MainPassengerName;
                booking.BookingRequest = JsonConvert.SerializeObject(bookingRequestInternal);
            }

            void AddServiceDetails()
            {
                var rate = availabilityInfo.RoomContractSet.Rate;
                booking.TotalPrice = rate.FinalPrice.Amount;
                booking.Currency = rate.Currency;
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
                            r.Rate.FinalPrice, 
                            r.BoardBasis,
                            r.MealPlan,
                            r.Deadline.Date,
                            r.ContractDescription,
                            r.Remarks,
                            GetDeadline(r.Deadline),
                            GetCorrespondingPassengers(number),
                            string.Empty))
                    .ToList();
                
                List<Passenger> GetCorrespondingPassengers(int number) => bookingRequestRoomDetails[number].Passengers
                    .Select(p=> new Passenger(p.Title, p.LastName, p.FirstName, p.IsLeader, p.Age))
                    .ToList();


                Deadline GetDeadline(EdoContracts.Accommodations.Deadline deadline)
                {
                    var policies = deadline.Policies
                        .Select(p => new Data.Booking.CancellationPolicy(p.FromDate, p.Percentage)).ToList();
                    
                    return new Deadline(deadline.Date, policies, deadline.Remarks);
                }
            }
        }
    }
}
