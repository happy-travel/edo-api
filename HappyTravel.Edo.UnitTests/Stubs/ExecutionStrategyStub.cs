using System;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Edo.UnitTests.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace HappyTravel.Edo.UnitTests.Stubs
{
    class ExecutionStrategyStub : IExecutionStrategy
    {
        public TResult Execute<TState, TResult>(
            TState state, Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>> verifySucceeded
        )
            => throw new NotImplementedException();


        public Task<TResult> ExecuteAsync<TState, TResult>(
            TState state, 
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation, 
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, 
            CancellationToken cancellationToken = new CancellationToken())
        {
            var edoContextMock = MockEdoContextFactory.Create();
            var dbFacade = new Mock<DatabaseFacade>(edoContextMock.Object);
            dbFacade.Setup(d => d.CurrentTransaction).Returns((IDbContextTransaction)null);

            edoContextMock.Setup(c => c.Database).Returns(dbFacade.Object);

            return operation(edoContextMock.Object, default, CancellationToken.None);
        }


        public bool RetriesOnFailure { get; }
    }
}
