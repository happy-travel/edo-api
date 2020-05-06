using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Availabilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;


namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public class AvailabilityResultsAggregator : BackgroundService
    {
        private readonly IDoubleFlow _doubleFlow;
        private readonly ConcurrentDictionary<Guid, Task<Result<CombinedAvailabilityDetails, ProblemDetails>>> _availabilityTasks = new ConcurrentDictionary<Guid, Task<Result<CombinedAvailabilityDetails, ProblemDetails>>>();


        public AvailabilityResultsAggregator(IDoubleFlow doubleFlow)
        {
            _doubleFlow = doubleFlow;
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }


        public AvailabilityRequestTicket ScheduleSearch(IAvailabilityService availabilityService, AvailabilityRequest request, string languageCode)
        {
            var ticket = new AvailabilityRequestTicket(Guid.NewGuid());
            _availabilityTasks.AddOrUpdate(ticket.Id, availabilityService.GetAvailable(request, languageCode), (guid, task) => task);
            return ticket;
        }


        public async Task<CombinedAvailabilityDetails> GetResult(Guid tickedId)
        {
            return await _doubleFlow.GetAsync<CombinedAvailabilityDetails>(tickedId.ToString(), TimeSpan.Zero);
            
        }
        
        public AvailabilityRequestStatus GetStatus(Guid tickedId)
        {
            if(!_availabilityTasks.TryGetValue(tickedId, out var availabilityTask))
                return new AvailabilityRequestStatus(false);

            switch (availabilityTask.Status)
            {
                case TaskStatus.Running: return new AvailabilityRequestStatus(false);
                case TaskStatus.RanToCompletion: return new AvailabilityRequestStatus(true);
            }
            throw new Exception();
        }


        private async Task Start(IAvailabilityService availabilityService, AvailabilityRequest request, string languageCode, AvailabilityRequestTicket ticket)
        {
            var (_, isFailure, results, error) = await availabilityService
                .GetAvailable(request, languageCode);

            await _doubleFlow.SetAsync(ticket.Id.ToString(), results, TimeSpan.FromHours(1));
        }
        
    }

    public readonly struct AvailabilityRequestTicket
    {
        public Guid Id { get; }


        public AvailabilityRequestTicket(Guid id)
        {
            Id = id;
        }
    }

    public readonly struct AvailabilityRequestStatus
    {
        public bool IsCompleted { get; }


        public AvailabilityRequestStatus(bool isCompleted)
        {
            IsCompleted = isCompleted;
        }
    }
    
    //public readonly struct 
}