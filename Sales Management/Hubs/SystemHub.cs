using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Sales_Management.Hubs
{
    public class SystemHub : Hub
    {
        public async Task SendUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveUpdate", message);
        }
    }
}
