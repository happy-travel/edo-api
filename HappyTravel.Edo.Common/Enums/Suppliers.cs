namespace HappyTravel.Edo.Common.Enums
{
    // DO NOT ADD DESCRIPTIONS AND JSON SERIALIZING SETTINGS THERE
    // This enum should be serialized and deserialized as Int32 to hide real provider names from client.
    public enum Suppliers
    {
        Unknown = 0,
        Netstorming = 1,
        Illusions = 2,
        DirectContracts = 3,
        Etg = 4,
        Rakuten = 5
    }
}