using System.Threading.Tasks;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Markups.MarkupServiceTests
{
    public class ClassUnderMarkup
    {
        public MoneyAmount Price { get; set; }

        public static async ValueTask<ClassUnderMarkup> Apply(ClassUnderMarkup initial, PriceProcessFunction processFunction)
        {
            var resultPrice = await processFunction(initial.Price);
            return new ClassUnderMarkup
            {
                Price = resultPrice
            };
        }
    }
}