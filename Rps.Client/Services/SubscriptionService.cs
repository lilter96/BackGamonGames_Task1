using Rps.Api.Grpc;

namespace Rps.Client.Services;

public class SubscriptionService
{
    public event Action<GameEvent> OnGameEvent;

    public async Task SubscribeAsync(GrpcClient grpcClient, int userId, string roomName, CancellationToken cancellationToken)
    {
        await foreach (var ev in grpcClient.SubscribeGameAsync(userId, roomName, cancellationToken))
        {
            OnGameEvent?.Invoke(ev);
        }
    }
}
