using System;
using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Documents;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.Money.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments
{
    public readonly struct PaymentReceipt
    {
        public PaymentReceipt(decimal amount, Currencies currency, 
            PaymentTypes method, string referenceCode, DocumentRegistrationInfo invoiceInfo, string accommodationName, DateTime checkInDate,
            DateTime checkOutDate, DateTime? deadlineDate, List<ReceiptItemInfo> receiptItems, BuyerInfo buyerDetails, string clientName = default)
        {
            Amount = amount;
            Currency = currency;
            Method = method;
            ReferenceCode = referenceCode;
            InvoiceInfo = invoiceInfo;
            CustomerName = clientName ?? string.Empty;
            BuyerDetails = buyerDetails;
            AccommodationName = accommodationName;
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            DeadlineDate = deadlineDate;
            ReceiptItems = receiptItems;
        }


        public decimal Amount { get; }
        public Currencies Currency { get; }
        public PaymentTypes Method { get; }
        public string ReferenceCode { get; }
        public DocumentRegistrationInfo InvoiceInfo { get; }
        public string CustomerName { get; }
        public BuyerInfo BuyerDetails { get; }
        public string AccommodationName { get; }
        public DateTime CheckInDate { get; }
        public DateTime CheckOutDate { get; }
        public DateTime? DeadlineDate { get; }
        public List<ReceiptItemInfo> ReceiptItems { get; }


        public readonly struct BuyerInfo
        {
            [JsonConstructor]
            public BuyerInfo(string name, string address, string contactPhone, string email)
            {
                Address = address;
                ContactPhone = contactPhone;
                Email = email;
                Name = name;
            }


            public string Address { get; }
            public string ContactPhone { get; }
            public string Email { get; }
            public string Name { get; }
        }


        public readonly struct ReceiptItemInfo
        {
            [JsonConstructor]
            public ReceiptItemInfo(DateTime? deadlineDate, RoomTypes roomType)
            {
                DeadlineDate = deadlineDate;
                RoomType = roomType;
            }

            public DateTime? DeadlineDate { get; }
            public RoomTypes RoomType { get; }
        }
    }
}