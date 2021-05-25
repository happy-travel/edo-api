using System.Threading.Tasks;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Services.SupplierOrders
{
    public interface ISupplierOrderService
    {
        Task Add(string referenceCode, ServiceTypes serviceType, MoneyAmount convertedPrice, MoneyAmount supplierPrice, Suppliers supplier);
    }
}