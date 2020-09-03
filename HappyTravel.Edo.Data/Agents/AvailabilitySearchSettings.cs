using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.Agents
{
    public class AvailabilitySearchSettings
    {
        /// <summary>
        /// Enabled providers list
        /// </summary>
        public List<DataProviders> EnabledProviders { get; set; }
    }
}