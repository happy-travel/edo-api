using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Suppliers;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Services.SupplierOrders
{
    public class SupplierOrderService : ISupplierOrderService
    {
        public SupplierOrderService(EdoContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }


        public async Task Add(string referenceCode, ServiceTypes serviceType, MoneyAmount convertedSupplierPrice, MoneyAmount originalSupplierPrice, Suppliers supplier)
        {
            var now = _dateTimeProvider.UtcNow();
            var supplierOrder = new SupplierOrder
            {
                Created = now,
                Modified = now,
                ConvertedSupplierPrice = convertedSupplierPrice.Amount,
                ConvertedSupplierCurrency = convertedSupplierPrice.Currency,
                OriginalSupplierPrice = originalSupplierPrice.Amount,
                OriginalSupplierCurrency = originalSupplierPrice.Currency,
                Supplier = supplier,
                Type = serviceType,
                ReferenceCode = referenceCode
            };

            _context.SupplierOrders.Add(supplierOrder);
            
            await _context.SaveChangesAsync();
            _context.Detach(supplierOrder);
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}