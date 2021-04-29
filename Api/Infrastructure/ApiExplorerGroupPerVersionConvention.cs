using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Linq;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public class ApiExplorerGroupPerVersionConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var controllerNamespace = controller.ControllerType.Namespace;
            var controllerPurpose = controllerNamespace.Split('.').Last();

            switch (controllerPurpose)
            {
                case "AdministratorControllers":
                    controller.ApiExplorer.GroupName = "admin";
                    return;
                case "AgentControllers":
                    controller.ApiExplorer.GroupName = "agent";
                    return;
                case "Controllers":
                    controller.ApiExplorer.GroupName = "service";
                    return;
            }
        }
    }
}
