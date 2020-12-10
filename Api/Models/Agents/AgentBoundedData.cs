using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Agents
{
    public readonly struct AgentBoundedData<TData>
    {
        [JsonConstructor]
        public AgentBoundedData(SlimAgentDescription agent, TData data)
        {
            Agent = agent;
            Data = data;
        }
        
        /// <summary>
        /// Nested data
        /// </summary>
        public TData Data { get; init; }
        
        /// <summary>
        /// Slim agent information
        /// </summary>
        public SlimAgentDescription Agent { get; init; }
    }
}