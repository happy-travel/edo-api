using System;
using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/reports")]
    [Produces("application/json")]
    public class ReportsController : BaseController
    {
        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }
        
        
        /// <summary>
        ///     Returns supplier wise direct connectivity report
        /// </summary>
        [HttpGet("direct-connectivity-report/supplier-wise")]
        [ProducesResponseType(typeof(FileStream), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.ReportGeneration)]
        public async Task<IActionResult> GetSupplerWiseDirectConnectivityReport(DateTime from, DateTime end)
        {
            var (_, isFailure, stream, error) = await _reportService.GetSupplierWiseReport(from, end);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return new FileStreamResult(stream, new MediaTypeHeaderValue("text/csv"))
            {
                FileDownloadName = $"direct-connectivity-report-suppliers-{from:g}-{end:g}.csv"
            };
        }
        
        
        /// <summary>
        ///     Returns agency wise direct connectivity report
        /// </summary>
        [HttpGet("direct-connectivity-report/agency-wise")]
        [ProducesResponseType(typeof(FileStream), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.ReportGeneration)]
        public async Task<IActionResult> GetAgencyWiseDirectConnectivityReport(DateTime from, DateTime end)
        {
            var (_, isFailure, stream, error) = await _reportService.GetAgencyWiseReport(from, end);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return new FileStreamResult(stream, new MediaTypeHeaderValue("text/csv"))
            {
                FileDownloadName = $"direct-connectivity-report-agencies-{from:g}-{end:g}.csv"
            };
        }
        
        
        /// <summary>
        ///     Returns agencies productivity report
        /// </summary>
        [HttpGet("agencies-productivity-report")]
        [ProducesResponseType(typeof(FileStream), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.ReportGeneration)]
        public async Task<IActionResult> GetAgenciesProductivityReport(DateTime from, DateTime end)
        {
            var (_, isFailure, stream, error) = await _reportService.AgenciesProductivityReport(from, end);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return new FileStreamResult(stream, new MediaTypeHeaderValue("text/csv"))
            {
                FileDownloadName = $"agencies-productivity-report-{from:g}-{end:g}.csv"
            };
        }
        
        
        /// <summary>
        ///     Returns full bookings report
        /// </summary>
        [HttpGet("full-bookings-report")]
        [ProducesResponseType(typeof(FileStream), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.ReportGeneration)]
        public async Task<IActionResult> GetFullBookingReport(DateTime from, DateTime end)
        {
            var (_, isFailure, stream, error) = await _reportService.GetFullBookingsReport(from, end);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return new FileStreamResult(stream, new MediaTypeHeaderValue("text/csv"))
            {
                FileDownloadName = $"full-bookings-report-{from:g}-{end:g}.csv"
            };
        }


        private readonly IReportService _reportService;
    }
}