using System.Threading.Tasks;

namespace HappyTravel.Edo.NotificationCenter.Services.Hubs
{
    public interface ISearchHub
    {
        Task SearchStateChanged();
    }
}