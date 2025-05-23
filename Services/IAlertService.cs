using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace WellbeingTeamsBot.Services
{
    public interface IAlertService
    {
        Task SendCardAsync(string objectId, JObject adaptiveCardJson);
    }
}
