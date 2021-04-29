using System;
using System.Linq;

namespace HappyTravel.Edo.Api.Services.Reports.RecordManagers
{
    public interface IRecordManager<out TProjection>
    {
        IQueryable<TProjection> Get(DateTime fromDate, DateTime endDate);
    }
}