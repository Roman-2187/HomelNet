using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace HomeNetServer.Hubs
{
    /// <summary>
    /// Распределенный сетевой автобус SiberNet.
    /// Перенаправляет события между удаленными машинами (Ryzen / Dell) в реальном времени.
    /// </summary>
    public class ChatHub : Hub
    {
        // При подключении привязываем юзера к его уникальному ID
        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
