using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.Extensions;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Availability
{
    public class DeadlinePolicyProcessorTests
    {
        [Fact]
        public void Positive_shift_should_be_made_zero_in_deadline_date()
        {
            var originalDeadlineDate = new DateTime(2021, 1, 15);
            var originalDeadline = new Deadline(originalDeadlineDate);
            var checkIn = originalDeadlineDate;
            var shiftSpan = TimeSpan.FromDays(10);
            var settings = new CancellationPolicyProcessSettings { PolicyStartDateShift = shiftSpan };

            var shiftedDeadline = DeadlinePolicyProcessor.Process(originalDeadline, checkIn, settings);

            Assert.Equal(originalDeadline, shiftedDeadline);
        }


        [Fact]
        public void Positive_shift_should_be_made_zero_to_policies()
        {
            var originalDeadlineDate = new DateTime(2021, 1, 15);
            var shiftSpan = TimeSpan.FromDays(10);
            var originalDeadline = new Deadline(originalDeadlineDate, Policies);
            var checkIn = originalDeadlineDate;
            var settings = new CancellationPolicyProcessSettings { PolicyStartDateShift = shiftSpan };

            var shiftedDeadline = DeadlinePolicyProcessor.Process(originalDeadline, checkIn, settings);

            for(int i = 0; i < originalDeadline.Policies.Count; i++)
                Assert.Equal(originalDeadline.Policies[i].FromDate, shiftedDeadline.Policies[i].FromDate);
        }


        [Fact]
        public void Shift_should_change_date_by_exact_days_count_in_deadline_date()
        {
            var originalDeadlineDate = new DateTime(2021, 1, 15);
            var expectedShiftedDate = new DateTime(2021, 1, 5);
            var shiftSpan = TimeSpan.FromDays(-10);
            var originalDeadline = new Deadline(originalDeadlineDate);
            var checkIn = originalDeadlineDate;
            var settings = new CancellationPolicyProcessSettings { PolicyStartDateShift = shiftSpan };

            var shiftedDeadline = DeadlinePolicyProcessor.Process(originalDeadline, checkIn, settings);

            Assert.Equal(expectedShiftedDate, shiftedDeadline.Date);
        }


        [Fact]
        public void Shift_should_change_date_by_exact_days_count_to_policies()
        {
            var originalDeadlineDate = new DateTime(2021, 1, 15);
            var shiftSpan = TimeSpan.FromDays(-10);
            var expectedShiftedDates = new List<DateTime>
            {
                new DateTime(2021, 1, 6),
                new DateTime(2021, 1, 7),
                new DateTime(2021, 1, 8),
                new DateTime(2021, 1, 9),
            };
            var originalDeadline = new Deadline(originalDeadlineDate, Policies);
            var checkIn = originalDeadlineDate;
            var settings = new CancellationPolicyProcessSettings { PolicyStartDateShift = shiftSpan };

            var shiftedDeadline = DeadlinePolicyProcessor.Process(originalDeadline, checkIn, settings);

            for (int i = 0; i < originalDeadline.Policies.Count; i++)
                Assert.Equal(expectedShiftedDates[i], shiftedDeadline.Policies[i].FromDate);
        }


        [Fact]
        public void Shift_should_keep_percentage_in_policies()
        {
            var originalDeadlineDate = new DateTime(2021, 1, 15);
            var shiftSpan = TimeSpan.FromDays(-10);
            var originalDeadline = new Deadline(originalDeadlineDate, Policies);
            var checkIn = originalDeadlineDate;
            var settings = new CancellationPolicyProcessSettings { PolicyStartDateShift = shiftSpan };

            var shiftedDeadline = DeadlinePolicyProcessor.Process(originalDeadline, checkIn, settings);

            for (int i = 0; i < originalDeadline.Policies.Count; i++)
                Assert.Equal(originalDeadline.Policies[i].Percentage, shiftedDeadline.Policies[i].Percentage);
        }


        [Fact]
        public void Shift_should_use_check_in_if_deadline_null()
        {
            var originalDeadlineDate = new DateTime(2021, 1, 15);
            var shiftSpan = TimeSpan.FromDays(0);
            var originalDeadline = new Deadline(null);
            var checkIn = originalDeadlineDate;
            var settings = new CancellationPolicyProcessSettings { PolicyStartDateShift = shiftSpan };

            var shiftedDeadline = DeadlinePolicyProcessor.Process(originalDeadline, checkIn, settings);

            Assert.Equal(originalDeadlineDate, shiftedDeadline.Date);
        }


        private static readonly List<CancellationPolicy> Policies = new List<CancellationPolicy>
        {
            new CancellationPolicy(new DateTime(2021, 1, 16), 1d),
            new CancellationPolicy(new DateTime(2021, 1, 17), 2d),
            new CancellationPolicy(new DateTime(2021, 1, 18), 3d),
            new CancellationPolicy(new DateTime(2021, 1, 19), 4d),
        };
    }
}
