using System.Runtime.CompilerServices;
using Grpc.Net.Client;
using Rps.Api.Grpc;

namespace Rps.Client.Services
{
    public class GrpcClient : IDisposable
    {
        private readonly GameService.GameServiceClient _client;
        private readonly GrpcChannel _channel;

        public GrpcClient(string grpcAddress)
        {
            _channel = GrpcChannel.ForAddress(grpcAddress);
            _client = new GameService.GameServiceClient(_channel);
        }

        public async Task<int> RegisterUserAsync(string username)
        {
            var reply = await _client.RegisterUserAsync(new RegisterUserRequest { Username = username });
            return reply.UserId;
        }

        public async Task<decimal> GetBalanceAsync(int userId)
        {
            var reply = await _client.GetBalanceAsync(new UserRequest { UserId = userId });
            return (decimal)reply.Balance;
        }

        public async Task<IEnumerable<OpenMatchDto>> GetOpenMatchesAsync()
        {
            var reply = await _client.GetGamesAsync(new Empty());

            return reply
                .Games
                .Select(game => new OpenMatchDto(game.RoomName, (decimal)game.Bet, game.IsWaitingPlayer))
                .ToList();
        }

        public async Task<string> JoinMatchAsync(string roomName, int userId)
        {
            var reply = await _client.JoinByNameAsync(new JoinByNameRequest
            {
                UserId = userId,
                RoomName = roomName
            });

            return reply.Message;
        }

        public async Task<string> MakeMoveAsync(string roomName, int userId, MoveType move)
        {
            var reply = await _client.MakeMoveAsync(new MoveRequest
            {
                UserId = userId,
                RoomName = roomName,
                Move = move
            });

            return reply.Message;
        }

        public async IAsyncEnumerable<GameEvent> SubscribeGameAsync(int userId, string roomName, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var call = _client.SubscribeGame(new SubscribeRequest
            {
                UserId = userId,
                RoomName = roomName
            }, cancellationToken: cancellationToken);

            while (await call.ResponseStream.MoveNext(cancellationToken))
            {
                yield return call.ResponseStream.Current;
            }
        }

        public void Dispose()
        {
            _channel.Dispose();
        }
    }
}
