using System.Collections.Concurrent;
using Grpc.Core;
using Rps.Application.DTOs;
using Rps.Application.Interfaces;

namespace Rps.Api.Grpc;

public class GrpcGameService(IGameService gameService) : GameService.GameServiceBase
{
    private static readonly ConcurrentDictionary<string, List<IServerStreamWriter<GameEvent>>> Subscribers = new();

    public override async Task<RegisterUserReply> RegisterUser(RegisterUserRequest request, ServerCallContext context)
    {
        try
        {
            var userId = await gameService.RegisterUserAsync(request.Username);

            return new RegisterUserReply
            {
                UserId = userId,
                Message = $"User created with Id={userId}"
            };
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<BalanceReply> GetBalance(UserRequest request, ServerCallContext context)
    {
        try
        {
            var bal = await gameService.GetBalanceAsync(request.UserId);
            return new BalanceReply { Balance = (double)bal };
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GamesReply> GetGames(Empty request, ServerCallContext context)
    {
        var open = await gameService.GetOpenMatchesAsync();
        var reply = new GamesReply();

        foreach (var (room, bet, isWait) in open)
        {
            reply.Games.Add(new GameInfo
            {
                RoomName = room,
                Bet = (double)bet,
                IsWaitingPlayer = isWait
            });
        }

        return reply;
    }

    public override async Task<JoinGameReply> JoinByName(JoinByNameRequest request, ServerCallContext context)
    {
        try
        {
            await gameService.JoinMatchAsync(request.RoomName, request.UserId);
            BroadcastEvent(request.RoomName, EventType.PlayerJoined, string.Empty);
            return new JoinGameReply { Message = string.Empty };
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<MoveReply> MakeMove(MoveRequest request, ServerCallContext context)
    {
        try
        {
            var result = await gameService.MakeMoveAsync(request.RoomName, request.UserId, (Domain.Enums.MoveType)request.Move);
            string message;

            if (result.Status == GameStatus.Finished)
            {
                message = result.WinnerId == null
                    ? "Draw"
                    : $"User {result.WinnerId} won";

                BroadcastEvent(request.RoomName, EventType.GameEnded, message);
            }
            else
            {
                message = $"User {request.UserId} moved '{request.Move}'";
                BroadcastEvent(request.RoomName, EventType.MoveMade, message);
            }

            return new MoveReply { Message = message };
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task SubscribeGame(SubscribeRequest request, IServerStreamWriter<GameEvent> responseStream,
        ServerCallContext context)
    {
        var room = request.RoomName;
        Subscribers.AddOrUpdate(room,
            _ => [responseStream],
            (_, lst) =>
            {
                lst.Add(responseStream);
                return lst;
            });

        await responseStream.WriteAsync(new GameEvent
        {
            EventType = EventType.Info,
            Message = $"Subscribed to room '{room}'"
        });

        try
        {
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
        }
        catch (TaskCanceledException)
        {
        }
        finally
        {
            if (Subscribers.TryGetValue(room, out var subList))
            {
                subList.Remove(responseStream);
            }
        }
    }

    private static void BroadcastEvent(string room, EventType eventType, string msg)
    {
        if (!Subscribers.TryGetValue(room, out var list))
        {
            return;
        }

        var ev = new GameEvent
        {
            EventType = eventType,
            Message = msg
        };
        foreach (var stream in list.ToArray())
            _ = Task.Run(async () =>
            {
                try
                {
                    await stream.WriteAsync(ev);
                }
                catch
                {
                    // ignored
                }
            });
    }
}
