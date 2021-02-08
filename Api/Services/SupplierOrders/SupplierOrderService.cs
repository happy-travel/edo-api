using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.SupplierOrders
{
    public class SupplierOrderService : ISupplierOrderService
    {
        public SupplierOrderService(EdoContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }


        public async Task Add(string referenceCode, ServiceTypes serviceType, decimal supplierPrice, Suppliers supplier)
        {
            var now = _dateTimeProvider.UtcNow();
            var supplierOrder = new SupplierOrder
            {
                Created = now,
                Modified = now,
                Price = supplierPrice,
                State = SupplierOrderState.Created,
                Supplier = supplier,
                Type = serviceType,
                ReferenceCode = referenceCode
            };

            _context.SupplierOrders.Add(supplierOrder);
            
            await _context.SaveChangesAsync();
            _context.Detach(supplierOrder);
        }


        public async Task Cancel(string referenceCode)
        {
            var orderToCancel = await _context.SupplierOrders
                .SingleOrDefaultAsync(o => o.ReferenceCode == referenceCode);

            if (orderToCancel == default)
                return;

            orderToCancel.State = SupplierOrderState.Canceled;
            _context.SupplierOrders.Update(orderToCancel);
            await _context.SaveChangesAsync();
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}