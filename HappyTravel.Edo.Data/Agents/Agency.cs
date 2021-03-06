using System;
using System.Collections.Generic;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Data.Agents
{
    public class Agency
    {
        public int Id { get; set; }
        public int CounterpartyId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string CountryCode { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string PostalCode { get; set; }
        public Currencies PreferredCurrency { get; set; }
        public string VatNumber { get; set; }
        public string BillingEmail { get; set; }
        public string Website { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public int? ParentId { get; set; }
        public bool IsActive { get; set; }
        public List<int> Ancestors { get; init; } = new();
    }
}