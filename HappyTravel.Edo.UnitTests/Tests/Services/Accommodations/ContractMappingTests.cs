using System;
using System.Collections.Generic;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations
{
    public class ContractMappingTests
    {
        [Fact]
        void Count_cancellation_policies_should_be_equal()
        {
            var sourceContractSet = GetRoomContractSet();
            var resultContractSet = sourceContractSet.ToRoomContractSet(null, isDirectContract: false, creditCardPaymentCommission: default);
            
            Assert.Equal(5, resultContractSet.Deadline.Policies.Count);
        }
        
        
        [Fact]
        void Date_of_deadline_should_be_minimal()
        {
            var sourceContractSet = GetRoomContractSet();
            var resultContractSet = sourceContractSet.ToRoomContractSet(null, isDirectContract: false, creditCardPaymentCommission: default);
            
            Assert.Equal(new DateTime(1, 1, 1), resultContractSet.Deadline.Date);
        }


        [Fact]
        void Percent_in_cancellation_policies_should_be_equal()
        {
            var sourceContractSet = GetRoomContractSet();
            var resultContractSet = sourceContractSet.ToRoomContractSet(null, isDirectContract: false, creditCardPaymentCommission: default);

            // totalAmount = 3818
            // 5 jan - ((279 * 50) + (3483 * 50)) / 3818 = 49.2666317444
            Assert.True(IsEqual(49.2666317444, resultContractSet.Deadline.Policies[0].Percentage));
            
            // 10 jan - ((279 * 75) + (3483 * 70)) / 3818 = 69.3386589838
            Assert.True(IsEqual(69.3386589838, resultContractSet.Deadline.Policies[1].Percentage));
            
            // 15 jan - ((279 * 90) + (3483 * 90)) / 3818 = 88.6799371399
            Assert.True(IsEqual(88.6799371399, resultContractSet.Deadline.Policies[2].Percentage));
            
            // 20 jan - ((279 * 90) + (3483 * 100)) / 3818 = 97.8025144054
            Assert.True(IsEqual(97.8025144054, resultContractSet.Deadline.Policies[3].Percentage));
            
            // 25 jan - ((279 * 100) + (3483 * 100)) / 3818 = 98.5332634887
            Assert.True(IsEqual(98.5332634887, resultContractSet.Deadline.Policies[4].Percentage));


            static bool IsEqual(double expected, double actual) 
                => Math.Abs(actual - expected) < 0.0001;
        }


        private RoomContractSet GetRoomContractSet()
        {
            var roomContracts = new List<RoomContract>();
            for (var i = 0; i < 3; i++)
            {
                roomContracts.Add(new RoomContract(
                    default, 
                    string.Empty, 
                    default, 
                    default, 
                    default, 
                    string.Empty, 
                    new List<KeyValuePair<string, string>>(), 
                    new List<DailyRate>(), 
                    _rates[i], 
                    default, 
                    deadline: _deadlines[i]));
            }
            
            return new RoomContractSet(
                Guid.NewGuid(),
                default,
                default,
                roomContracts,
                new List<string>(),
                false);
        }


        private readonly Dictionary<int, Deadline> _deadlines = new()
        {
            { 0, new Deadline(null, new List<CancellationPolicy>
            {
                { new(new DateTime(1, 1, 5), 50) },
                { new(new DateTime(1, 1, 10), 100) },
            }, new List<string>(), false) },
            { 1, new Deadline(new DateTime(1, 1, 1), new List<CancellationPolicy>
            {
                { new(new DateTime(1, 1, 5), 50) },
                { new(new DateTime(1, 1, 10), 75) },
                { new(new DateTime(1, 1, 15), 90) },
                { new(new DateTime(1, 1, 25), 100) },
            }, new List<string>(), false) },
            { 2, new Deadline(new DateTime(1, 1, 1), new List<CancellationPolicy>
            {
                { new(new DateTime(1, 1, 5), 50) },
                { new(new DateTime(1, 1, 10), 70) },
                { new(new DateTime(1, 1, 15), 90) },
                { new(new DateTime(1, 1, 20), 100) },
            }, new List<string>(), false) },
        };


        private readonly Dictionary<int, Rate> _rates = new()
        {
            { 0, new Rate(new MoneyAmount(56, Currencies.USD), default) },
            { 1, new Rate(new MoneyAmount(279, Currencies.USD), default) },
            { 2, new Rate(new MoneyAmount(3483, Currencies.USD), default) },
        };
    }
}