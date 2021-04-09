using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management
{
    public interface IBookingRecordsUpdater
    {
        Task<Result> ChangeStatus(Booking booking, BookingStatuses status, DateTime date, ApiCaller apiCaller, BookingChangeReason reason);

        Task UpdateWithSupplierData(Booking booking, string supplierReferenceCode, BookingUpdateModes updateModes,
            List<SlimRoomOccupation> updatedRooms);
    }
}