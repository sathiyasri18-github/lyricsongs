using Microsoft.AspNetCore.SignalR;

namespace Lyricsongs.Api.Hubs;

/// <summary>
/// Push render progress to the Angular client.
/// </summary>
public sealed class RenderProgressHub : Hub
{
    public Task JoinProjectGroup(string projectId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectId));

    public Task LeaveProjectGroup(string projectId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(projectId));

    public static string ProjectGroup(string projectId) => $"project:{projectId}";
}
