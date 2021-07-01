namespace HappyTravel.Edo.Common.Enums
{
    public enum ManagementEventType
    {
        None = 0,
        CounterpartyVerification = 10,
        AdministratorRegistration = 20,
        CounterpartyDeactivation = 30,
        AgencyDeactivation = 40,
        CounterpartyActivation = 50,
        AgencyActivation = 60,
        AgentMovement = 70,
        CounterpartyAccountDeactivation = 80,
        CounterpartyAccountActivation = 90,
        AgencyAccountDeactivation = 100,
        AgencyAccountActivation = 110,
        AgencySystemSettingsCreateOrEdit = 120,
        AgencySystemSettingsDelete = 130,
        DiscountCreate = 140,
        DiscountEdit = 150,
        DiscountDelete = 151,
        AgentSystemSettingsCreateOrEdit = 160,
        AgentSystemSettingsDelete = 170,
        AgentApiClientCreateOrEdit = 180,
        AgentApiClientDelete = 190,
        CounterpartyEdit = 200,
        AdministratorRolesAssignment = 210,
    }
}