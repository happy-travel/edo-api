using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.AccommodationMappings;
using HappyTravel.Edo.Data.Management;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Management
{
    public class AccommodationDuplicateReportsManagementService : IAccommodationDuplicateReportsManagementService
    {
        public AccommodationDuplicateReportsManagementService(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            ISupplierConnectorManager supplierConnectorManager)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _supplierConnectorManager = supplierConnectorManager;
        }


        public Task<List<SlimAccommodationDuplicateReportInfo>> Get()
        {
            return (from report in _context.AccommodationDuplicateReports
                join agent in _context.Agents on report.ReporterAgentId equals agent.Id
                orderby report.Created descending
                select new SlimAccommodationDuplicateReportInfo(report.Id,
                    report.Created,
                    report.ApprovalState,
                    agent.FirstName,
                    report.Accommodations))
                .ToListAsync();
        }


        public async Task<Result<AccommodationDuplicateReportInfo>> Get(int reportId, string languageCode)
        {
            var report = await _context.AccommodationDuplicateReports.SingleOrDefaultAsync(r => r.Id == reportId);
            if (report == default)
                return Result.Failure<AccommodationDuplicateReportInfo>("Could not find a report");

            var accommodations = new List<SupplierData<Accommodation>>(report.Accommodations.Count);
            foreach (var supplierAccommodationId in report.Accommodations)
            {
                var (_, isFailure, accommodationDetails, result) = await _supplierConnectorManager
                    .Get(supplierAccommodationId.Supplier)
                    .GetAccommodation(supplierAccommodationId.Id, languageCode);
                
                if (isFailure)
                    return Result.Failure<AccommodationDuplicateReportInfo>($"Could not find accommodation: {result.Detail}");
                
                accommodations.Add(SupplierData.Create(supplierAccommodationId.Supplier, accommodationDetails));
            }
            
            return new AccommodationDuplicateReportInfo(report.Id, report.Created, report.ApprovalState, accommodations);
        }


        public async Task<Result> Approve(int reportId, Administrator administrator)
        {
            return await ApproveReport(reportId)
                .Tap(ApproveDuplicates)
                .Tap(SaveChanges);

            
            async Task<Result<AccommodationDuplicateReport>> ApproveReport(int reportId)
            {
                var report = await _context.AccommodationDuplicateReports.SingleOrDefaultAsync(r => r.Id == reportId);
                if (report == default)
                    return Result.Failure<AccommodationDuplicateReport>("Could not find a report");
                
                report.ApprovalState = AccommodationDuplicateReportState.Approved;
                report.EditorAdministratorId = administrator.Id;
                report.Modified = _dateTimeProvider.UtcNow();
                _context.Update(report);
                return report;
            }


            async Task ApproveDuplicates(AccommodationDuplicateReport report)
            {
                var duplicates = await _context.AccommodationDuplicates.Where(d => d.ParentReportId == reportId).ToListAsync();
                foreach (var duplicate in duplicates)
                    duplicate.IsApproved = true;
                
                _context.UpdateRange(duplicates);
            }
        }
        
        
        
        public async Task<Result> Disapprove(int reportId, Administrator administrator)
        {
            return await DisapproveReport(reportId)
                .Tap(DisapproveDuplicates)
                .Tap(SaveChanges);
            
            async Task<Result<AccommodationDuplicateReport>> DisapproveReport(int reportId)
            {
                var report = await _context.AccommodationDuplicateReports.SingleOrDefaultAsync(r => r.Id == reportId);
                if (report == default)
                    return Result.Failure<AccommodationDuplicateReport>("Could not find a report");
                
                report.ApprovalState = AccommodationDuplicateReportState.Disapproved;
                report.EditorAdministratorId = administrator.Id;
                report.Modified = _dateTimeProvider.UtcNow();
                _context.Update(report);
                return report;
            }


            async Task DisapproveDuplicates(AccommodationDuplicateReport report)
            {
                var duplicates = await _context.AccommodationDuplicates.Where(d => d.ParentReportId == reportId).ToListAsync();
                foreach (var duplicate in duplicates)
                    duplicate.IsApproved = true;
                
                _context.UpdateRange(duplicates);
            }
        }
        
        private Task SaveChanges() => _context.SaveChangesAsync();
        
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ISupplierConnectorManager _supplierConnectorManager;
    }
}