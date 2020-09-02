using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.Mailing;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.Edo.Data.Markup;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.UnitTests.Mocks;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Bookings.BookingServiceTests
{
    public class BookingServiceTests : IDisposable
    {
        public BookingServiceTests(Mock<EdoContext> edoContextMock)
        {
            var entityLockerMock = new Mock<IEntityLocker>();

            entityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(Result.Ok()));

            _edoContextMock = edoContextMock;
            _mockedEdoContext = edoContextMock.Object;

            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(d => d.UtcNow()).Returns(_now);

            var loggerBookingRecordsManager = new Logger<BookingRecordsManager>(Mock.Of<ILoggerFactory>());
            var loggerBookingService = new Logger<BookingService>(Mock.Of<ILoggerFactory>());

            var bookingRecordsManager = new BookingRecordsManager(edoContextMock.Object, dateTimeProvider.Object, Mock.Of<ITagProcessor>(),
                Mock.Of<IAccommodationService>(), loggerBookingRecordsManager);

            _accountPaymentService = new Mock<IAccountPaymentService>();

            var strategy = new ExecutionStrategyMock();

            var dbFacade = new Mock<DatabaseFacade>(_mockedEdoContext);
            dbFacade.Setup(d => d.CreateExecutionStrategy()).Returns(strategy);
            edoContextMock.Setup(c => c.Database).Returns(dbFacade.Object);

            _bookingEvaluationStorage = new Mock<IBookingEvaluationStorage>();
            _bookingPaymentService = new Mock<IBookingPaymentService>();

            _dataProvider = new Mock<IDataProvider>();
            var dataProviderFactory = new Mock<IDataProviderFactory>();
            dataProviderFactory.Setup(d => d.Get(It.IsAny<DataProviders>())).Returns(_dataProvider.Object);

            _bookingService = new BookingService(_bookingEvaluationStorage.Object, bookingRecordsManager, Mock.Of<IBookingAuditLogService>(),
                Mock.Of<ISupplierOrderService>(), _mockedEdoContext, Mock.Of<IBookingMailingService>(), loggerBookingService,
                dataProviderFactory.Object, Mock.Of<IBookingDocumentsService>(), _bookingPaymentService.Object,
                Mock.Of<IOptions<DataProviderOptions>>(), Mock.Of<IPaymentNotificationService>(), _accountPaymentService.Object,
                Mock.Of<IAccountManagementService>(), dateTimeProvider.Object);

            _edoContextMock
                .Setup(c => c.Agencies)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Agency>
                {
                    new Agency
                    {
                        Id = 1,
                        Name = "Agency",
                        ParentId = null,
                    },
                }));

            _edoContextMock
                .Setup(c => c.AgencyAccounts)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<AgencyAccount>
                {
                    _account,
                }));

            _edoContextMock
                .Setup(c => c.Bookings)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Booking>
                {
                    //_booking,
                }));

            _edoContextMock
                .Setup(c => c.Payments)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Payment>
                {
                    _payment,
                }));

            _accommodationDeadlineNotPassed =
                new DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline>(
                    new SingleAccommodationAvailabilityDetailsWithDeadline("1", _checkInDate, _checkOutDate, _numberOfNights,
                        new AccommodationDetails(),
                        new RoomContractSet(new Guid(), new Price(), _notPassedDeadlineDate, new List<RoomContract>())),
                    new List<MarkupPolicy>()
                );

            _accommodationDeadlinePassed =
                new DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline>(
                    new SingleAccommodationAvailabilityDetailsWithDeadline("2", _checkInDate, _checkOutDate, _numberOfNights,
                        new AccommodationDetails(),
                        new RoomContractSet(new Guid(), new Price(), _passedDeadlineDate, new List<RoomContract>())),
                    new List<MarkupPolicy>()
                );
        }


        private void SetupBookingEvaluationStorage(DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline> accommodation) =>
            _bookingEvaluationStorage
                .Setup(s => s.Get(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<List<DataProviders>>()))
                .Returns(Task.FromResult(Result.Success((DataProviders.Netstorming, accommodation))));


        private void SetupPaymentResult(bool isSuccess) =>
            _accountPaymentService.Setup(a => a.Charge(It.IsAny<Booking>(), It.IsAny<AgentContext>(), It.IsAny<string>()))
                .Returns(Task.FromResult(isSuccess 
                    ? Result.Success(new PaymentResponse()) 
                    : Result.Failure<PaymentResponse>("err")));


        private void SetupBookResult(bool isSuccess, DateTime deadline) =>
            _dataProvider.Setup(d => d.Book(It.IsAny<BookingRequest>(), It.IsAny<string>()))
                .Returns( (BookingRequest r, string s) => Task.FromResult(isSuccess
                    ? Result.Success<BookingDetails, ProblemDetails>(new BookingDetails())
                    : Result.Failure<BookingDetails, ProblemDetails>(new ProblemDetails())));


        private BookingDetails BookingRequestToDetails(BookingRequest request, DateTime deadline) =>
            new BookingDetails(request.ReferenceCode, string.Empty, BookingStatusCodes.Confirmed, string.Empty,
                string.Empty, _checkInDate, _checkOutDate, string.Empty, deadline, string.Empty, string.Empty,
                new List<SlimRoomDetailsWithPrice>(), new BookingLocationDescription(), BookingUpdateMode.Synchronous);


        public void Dispose()
        {

        }
        
        /*private readonly Booking _booking = new Booking
        {
            Id = 1,
            Currency = Currencies.USD,
            AgencyId = 1,
            CounterpartyId = 1,
            AgentId = 1,
            ReferenceCode = "okay booking",
            TotalPrice = 100,
            PaymentMethod = PaymentMethods.BankTransfer,
            Status = BookingStatusCodes.Confirmed,
            PaymentStatus = BookingPaymentStatuses.Captured,
        };*/

        private readonly DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline> _accommodationDeadlineNotPassed;
        private readonly DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline> _accommodationDeadlinePassed;

        private readonly AgencyAccount _account = new AgencyAccount
        {
            Id = 1,
            Balance = 1000,
            Currency = Currencies.USD,
            AgencyId = 1,
            IsActive = true
        };

        private readonly Payment _payment = new Payment
        {
            Id = 1,
            BookingId = 1,
            Amount = 100,
            Status = PaymentStatuses.Captured,
        };

        private readonly BookingDetails _successfullBookingResponse = new BookingDetails();

        private readonly DateTime _now = new DateTime(2020, 1, 10);
        private readonly DateTime _checkInDate = new DateTime(2020, 1, 15);
        private readonly DateTime _checkOutDate = new DateTime(2020, 1, 20);
        private readonly DateTime _passedDeadlineDate = new DateTime(2020, 1, 8);
        private readonly DateTime _notPassedDeadlineDate = new DateTime(2020, 1, 12);

        private readonly int _numberOfNights = 5;

        private readonly Mock<EdoContext> _edoContextMock;
        private readonly EdoContext _mockedEdoContext;
        private readonly IBookingService _bookingService;
        private readonly AgentContext _agent = new AgentContext(1, "", "", "", "", "", 1, "", 1, true, InAgencyPermissions.All);
        private readonly Mock<IAccountPaymentService> _accountPaymentService;
        private readonly Mock<IBookingEvaluationStorage> _bookingEvaluationStorage;
        private readonly Mock<IBookingPaymentService> _bookingPaymentService;
        private readonly Mock<IDataProvider> _dataProvider;
    }
}
