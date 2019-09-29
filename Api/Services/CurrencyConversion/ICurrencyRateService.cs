using System.Threading.Tasks;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.CurrencyConversion
{
    public interface ICurrencyRateService
    {
        Task Set(Currencies source, Currencies target, decimal rate);
        ValueTask<decimal> Get(Currencies source, Currencies target);
    }
}