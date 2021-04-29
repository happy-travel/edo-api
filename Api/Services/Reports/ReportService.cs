using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using CsvHelper;
using HappyTravel.Edo.Api.Models.Reports;
using HappyTravel.Edo.Api.Models.Reports.DirectConnectivityReports;
using HappyTravel.Edo.Api.Services.Reports.Converters;
using HappyTravel.Edo.Api.Services.Reports.RecordManagers;
using HappyTravel.Edo.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.Edo.Api.Services.Reports
{
    public class ReportService : IReportService, IDisposable
    {
        public ReportService(EdoContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }


        public async Task<Result<Stream>> GetSupplierWiseReport(DateTime fromDate, DateTime endDate)
        {
            var from = fromDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);
            
            return await Validate(from, end)
                .Map(GetRecords)
                .Bind(Generate<SupplierWiseRecordProjection, SupplierWiseReportRow>);
            
            
            IQueryable<SupplierWiseRecordProjection> GetRecords()
                => GetRecords<SupplierWiseRecordProjection>(from, end);
        }


        public async Task<Result<Stream>> GetAgencyWiseReport(DateTime fromDate, DateTime endDate)
        {
            var from = fromDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);
            
            return await Validate(from, end)
                .Map(GetRecords)
                .Bind(Generate<AgencyWiseRecordProjection, AgencyWiseReportRow>);
            
            
            IQueryable<AgencyWiseRecordProjection> GetRecords()
                => GetRecords<AgencyWiseRecordProjection>(from, end);
        }


        public async Task<Result<Stream>> GetFullBookingsReport(DateTime fromDate, DateTime endDate)
        {
            var from = fromDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);
            
            return await Validate(from, end)
                .Map(GetRecords)
                .Bind(Generate<FullBookingsReportProjection, FullBookingsReportRow>);
            
            
            IQueryable<FullBookingsReportProjection> GetRecords()
                => GetRecords<FullBookingsReportProjection>(from, end);
        }


        public async Task<Result<Stream>> AgenciesProductivityReport(DateTime fromDate, DateTime endDate)
        {
            var from = fromDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);
            
            return await Validate(from, end)
                .Map(GetRecords)
                .Bind(Generate);
            
            
            IQueryable<AgencyProductivity> GetRecords()
                => GetRecords<AgencyProductivity>(from, end);
        }


        private Result Validate(DateTime fromDate, DateTime endDate)
        {
            if (fromDate == default || endDate == default)
                return Result.Failure("Range dates required");
            
            if ((fromDate - endDate).TotalDays > MaxDaysInReport)
                return Result.Failure<Stream>($"The interval for generating a report should not exceed {MaxDaysInReport} days");

            return Result.Success();
        }
        
        
        private IQueryable<TProjection> GetRecords<TProjection>(DateTime fromDate, DateTime endDate) 
            => _serviceProvider.GetRequiredService<IRecordManager<TProjection>>().Get(fromDate, endDate);


        private async Task<Result<Stream>> Generate<TProjection, TRow>(IEnumerable<TProjection> records)
        {
            var stream = await Initialize<TRow>();
            var converter = _serviceProvider.GetRequiredService<IConverter<TProjection, TRow>>();

            foreach (var record in records)
            {
                var row = converter.Convert(record, VatAmount, AmountExcludedVat);
                await Write(row);
            }
            
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
        
        
        private async Task<Result<Stream>> Generate<TRow>(IEnumerable<TRow> records)
        {
            var stream = await Initialize<TRow>();

            foreach (var record in records)
            {
                await Write(record);
            }
            
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }


        private async Task<Stream> Initialize<TRow>()
        {
            var stream = new MemoryStream();
            _streamWriter = new StreamWriter(stream);
            _csvWriter = new CsvWriter(_streamWriter, CultureInfo.InvariantCulture);
            
            _csvWriter.WriteHeader<TRow>();
            await _csvWriter.NextRecordAsync();
            await _streamWriter.FlushAsync();
            
            return stream;
        }


        private async Task Write<TRow>(TRow record)
        {
            _csvWriter.WriteRecord(record);
            await _csvWriter.NextRecordAsync();
            await _streamWriter.FlushAsync();
        }


        private static decimal VatAmount(decimal totalAmount) 
            => totalAmount * Vat / (100 + Vat);


        private static decimal AmountExcludedVat(decimal totalAmount) 
            => totalAmount / (1m + Vat / 100m);


        private const int Vat = 5;
        private const int MaxDaysInReport = 62;
        private CsvWriter _csvWriter;
        private StreamWriter _streamWriter;
        
        private readonly EdoContext _context;
        private readonly IServiceProvider _serviceProvider;


        public void Dispose()
        {
            _csvWriter?.Dispose();
            _streamWriter?.Dispose();
            _context?.Dispose();
        }
    }
}