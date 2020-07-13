using System;
using HappyTravel.Edo.Api.Infrastructure;

namespace HappyTravel.Edo.UnitTests.Stubs
{
    public class DateTimeProviderStub : IDateTimeProvider
    {
        public DateTimeProviderStub(DateTime dateTime)
        {
            _dateTime = dateTime;
        }


        private readonly DateTime _dateTime;

        public DateTime UtcNow() => _dateTime;
    }
}