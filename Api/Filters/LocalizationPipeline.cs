﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Filters
{
    
    public class LocalizationPipeline
    {
        public void Configure(IApplicationBuilder app, IOptions<RequestLocalizationOptions> options)
        {
            app.UseRequestLocalization(options.Value);
        }
    }
}
