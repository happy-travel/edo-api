using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.OData;

namespace HappyTravel.Edo.Api.Filters.OData
{
    public class EnablePaginatedQueryAttribute : EnableQueryAttribute
    {
        public override void ValidateQuery(HttpRequest httpRequest, ODataQueryOptions queryOptions)
        {
            if (queryOptions.Top is null)
                throw new ODataException("The 'top' option is required.");

            base.ValidateQuery(httpRequest, queryOptions);
        }
    }
}