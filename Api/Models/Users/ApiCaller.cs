using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Models.Users
{
    public readonly struct ApiCaller
    {
        public ApiCaller(int id, ApiCallerTypes type)
        {
            Id = id;
            Type = type;
        }


        public int Id { get; }
        public ApiCallerTypes Type { get; }
        
        public static ApiCaller InternalServiceAccount 
            => new(0, ApiCallerTypes.InternalServiceAccount);
        
        public static ApiCaller FromSupplier(Suppliers supplier) 
            => new((int) supplier, ApiCallerTypes.Supplier);
    }
}