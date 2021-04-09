using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Management;

namespace HappyTravel.Edo.Api.Models.Users
{
    public static class UserExtensions
    {
        public static ApiCaller ToUserInfo(this ServiceAccount serviceAccount) => new ApiCaller(serviceAccount.Id, ApiCallerTypes.ServiceAccount);
        
        public static ApiCaller ToUserInfo(this AgentContext agentContext) => new ApiCaller(agentContext.AgentId, ApiCallerTypes.Agent);
        
        public static ApiCaller ToUserInfo(this Administrator administrator) => new ApiCaller(administrator.Id, ApiCallerTypes.Admin);
    }
}