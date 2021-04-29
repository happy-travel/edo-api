using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.UnitTests.Utility;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.AdministratorServices.CounterpartyManagementServiceTests
{
    public class CounterpartyManagementTests : IDisposable
    {
        public CounterpartyManagementTests()
        {
            var mockCreationHelper = new AdministratorServicesMockCreationHelper();
            _context = mockCreationHelper.GetContextMock().Object;
            _counterpartyManagementService = mockCreationHelper.GetCounterpartyManagementService(_context);
        }


        [Fact]
        public async Task Get_specified_counterparty_should_return_counterparty_infо()
        {
            var (_, isFailure, counterparty, _) = await _counterpartyManagementService.Get(1);

            Assert.False(isFailure);
            Assert.True(counterparty.Name == "Test");
        }


        [Fact]
        public async Task Get_not_existed_counterparty_should_fail()
        {
            var (_, isFailure, _, _) = await _counterpartyManagementService.Get(7);
            
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Get_counterparty_should_return_all_counterparties()
        {
            var counterpartyList = await _counterpartyManagementService.Get();

            Assert.True(counterpartyList.Count == 5);
        }


        [Fact]
        public async Task Get_counterparty_agencies_should_return_agencies()
        {
            var agencies = await _counterpartyManagementService.GetAllCounterpartyAgencies(1, "AF");
            
            Assert.True(agencies.Count == 3);
        }


        [Fact]
        public async Task Get_not_existed_counterparty_agencies_should_return_empty_list()
        {
            var agencies = await _counterpartyManagementService.GetAllCounterpartyAgencies(7, "AF");

            Assert.False(agencies.Any());
        }


        [Fact]
        public async Task Counterparty_update_should_pass()
        {
            var counterpartyToUpdate = new CounterpartyEditRequest(
                name: "RenamedName",
                preferredPaymentMethod: PaymentTypes.Offline,
                vatNumber: "changed vatNumber"
            );

            var (_, isFailure, counterparty, error) = await _counterpartyManagementService.Update(counterpartyToUpdate, 1);

            Assert.False(isFailure);
            Assert.True(counterparty.Name == "RenamedName");
            Assert.True(_context.Counterparties.Single(c => c.Id == 1).Name != "Test" );
        }


        [Fact]
        public async Task Update_not_existing_counterparty_should_fail()
        {
            var counterpartyToUpdate = new CounterpartyEditRequest();
            
            var (_, isFailure, _, _) = await _counterpartyManagementService.Update(counterpartyToUpdate, 1);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Get_counterparties_predictions_should_return_right_data()
        {
            var predictions = await _counterpartyManagementService.GetCounterpartiesPredictions("pred");
            
            Assert.True(predictions.Single(pr=> pr.CounterpartyId ==14).BillingEmail == "predictionsExample@mail.com");
            Assert.True(predictions.Single(pr=> pr.CounterpartyId ==15).BillingEmail == "agentexample1@mail.com");
        }


        private readonly EdoContext _context;
        private readonly ICounterpartyManagementService _counterpartyManagementService;

        public void Dispose() { }
    }
}