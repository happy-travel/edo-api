using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Markups;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IAppliedBookingMarkupRecordsManager
    {
        Task Create(string referenceCode, IEnumerable<AppliedMarkup> appliedMarkups);
    }
}