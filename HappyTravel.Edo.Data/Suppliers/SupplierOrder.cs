using System;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Data.Suppliers
{
    public class SupplierOrder
    {
        public int Id { get; set; }
        public Common.Enums.Suppliers Supplier { get; set; }
        public decimal ConvertedSupplierPrice { get; set; }
        public Currencies ConvertedSupplierCurrency { get; set; }
        public decimal OriginalSupplierPrice { get; set; }
        public Currencies OriginalSupplierCurrency { get; set; }
        public ServiceTypes Type { get; set; }
        public string ReferenceCode { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}