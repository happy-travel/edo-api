using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Markup;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class MarkupLogger : IMarkupLogger
    {
        public MarkupLogger(EdoContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }


        public Task Write(string referenceCode, ServiceTypes serviceType, List<MarkupPolicy> policies)
        {
            _context.MarkupLog.Add(new AppliedMarkupLog
            {
                Policies = policies,
                ReferenceCode = referenceCode,
                ServiceType = serviceType,
                Created = _dateTimeProvider.UtcNow()
            });

            return _context.SaveChangesAsync();
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}