using Grpc.Core;
using Rps.Api.Grpc;

namespace Rps.Client.Services;

public class CommandHandler
    {
        private readonly GrpcClient _grpcClient;
        private readonly RestClient _restClient;
        private readonly SubscriptionService _subscriptionService;

        private int? _currentUserId;
        private string _currentRoom;
        private CancellationTokenSource _subscriptionCts;

        public CommandHandler(GrpcClient grpcClient, RestClient restClient, SubscriptionService subscriptionService)
        {
            _grpcClient = grpcClient;
            _restClient = restClient;
            _subscriptionService = subscriptionService;
            _subscriptionService.OnGameEvent += HandleGameEvent;
        }

        public async Task HandleCommandAsync(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var cmd = parts[0].ToLower();
            try
            {
                switch (cmd)
                {
                    case "help":
                        PrintHelp();
                        break;

                    case "register":
                        await RegisterAsync(parts);
                        break;

                    case "balance":
                        await ShowBalanceAsync();
                        break;

                    case "games":
                        await ListOpenGamesAsync();
                        break;

                    case "create":
                        await CreateMatchAsync(parts);
                        break;

                    case "join":
                        await JoinMatchAsync(parts);
                        break;

                    case "move":
                        await MakeMoveAsync(parts);
                        break;

                    case "transaction":
                        await CreateTransactionAsync(parts);
                        break;

                    case "unsubscribe":
                        Unsubscribe();
                        break;

                    default:
                        Console.WriteLine("Unknown command. Type 'help' for usage.");
                        break;
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument || ex.StatusCode == StatusCode.NotFound)
            {
                Console.WriteLine($"gRPC Error: {ex.Status.Detail}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Operation Error: {ex.Message}");
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid number format.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void PrintHelp()
        {
            Console.WriteLine(@"
Commands:

  register <username>          (gRPC) - Create user
  balance                      (gRPC) - Show current user balance
  games                        (gRPC) - List open games

  create <roomName> <bet>      (REST) - Create match with stake
  join <roomName>              (gRPC) - Join game by room name
  move <K|N|B>                 (gRPC) - Make your move (K=Rock, N=Scissors, B=Paper)
  unsubscribe                  (gRPC) - Stop receiving game events

  transaction <toUserId> <amt> (REST) - Send money to another user

  help                         - Show this help
  exit                         - Quit
");
        }

        private async Task RegisterAsync(string[] parts)
        {
            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: register <username>");
                return;
            }

            var username = parts[1];
            var userId = await _grpcClient.RegisterUserAsync(username);
            _currentUserId = userId;
            Console.WriteLine($"User registered successfully. User ID: {userId}");
        }

        private async Task ShowBalanceAsync()
        {
            if (_currentUserId == null)
            {
                Console.WriteLine("No current user. Register first.");
                return;
            }

            var balance = await _grpcClient.GetBalanceAsync(_currentUserId.Value);
            Console.WriteLine($"Balance: {balance}");
        }

        private async Task ListOpenGamesAsync()
        {
            var games = await _grpcClient.GetOpenMatchesAsync();
            Console.WriteLine("Open games:");
            foreach (var game in games)
            {
                Console.WriteLine($"  Room={game.RoomName}, Bet={game.Bet}, Waiting={game.IsWaiting}");
            }
        }

        private async Task CreateMatchAsync(string[] parts)
        {
            if (_currentUserId == null)
            {
                Console.WriteLine("No current user. Register first.");
                return;
            }

            if (parts.Length < 3)
            {
                Console.WriteLine("Usage: create <roomName> <bet>");
                return;
            }

            var roomName = parts[1];
            if (!decimal.TryParse(parts[2], out var bet))
            {
                Console.WriteLine("Invalid bet amount.");
                return;
            }

            var matchId = await _restClient.CreateMatchAsync(_currentUserId.Value, roomName, bet);
            Console.WriteLine($"Match was created. (MatchId={matchId})");
            _currentRoom = roomName;
            await SubscribeAsync();
        }

        private async Task JoinMatchAsync(string[] parts)
        {
            if (_currentUserId == null)
            {
                Console.WriteLine("No current user. Register first.");
                return;
            }

            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: join <roomName>");
                return;
            }

            var roomName = parts[1];
            var message = await _grpcClient.JoinMatchAsync(roomName, _currentUserId.Value);
            Console.WriteLine(message);
            _currentRoom = roomName;
            await SubscribeAsync();
        }

        private async Task MakeMoveAsync(string[] parts)
        {
            if (_currentUserId == null || string.IsNullOrEmpty(_currentRoom))
            {
                Console.WriteLine("Need userId and roomName (join first).");
                return;
            }

            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: move <K|N|B>");
                return;
            }

            var inputMove = parts[1].ToUpper();
            if (!TryParseMove(inputMove, out var move))
            {
                Console.WriteLine("Invalid move. Use K for Rock, N for Scissors, B for Paper.");
                return;
            }

            var result = await _grpcClient.MakeMoveAsync(_currentRoom, _currentUserId.Value, move);
            Console.WriteLine($"Move result: {result}");
        }

        private async Task CreateTransactionAsync(string[] parts)
        {
            if (_currentUserId == null)
            {
                Console.WriteLine("No current user. Register first.");
                return;
            }

            if (parts.Length < 3)
            {
                Console.WriteLine("Usage: transaction <toUserId> <amount>");
                return;
            }

            if (!int.TryParse(parts[1], out var toUserId))
            {
                Console.WriteLine("Invalid toUserId.");
                return;
            }

            if (!decimal.TryParse(parts[2], out var amount))
            {
                Console.WriteLine("Invalid amount.");
                return;
            }

            var message = await _restClient.CreateTransactionAsync(_currentUserId.Value, toUserId, amount);
            Console.WriteLine(message);
        }

        private void Unsubscribe()
        {
            _subscriptionCts?.Cancel();
            _subscriptionCts = null;
            Console.WriteLine("Unsubscribed from events.");
        }

        private async Task SubscribeAsync()
        {
            if (_subscriptionCts != null)
            {
                await _subscriptionCts.CancelAsync();
            }

            _subscriptionCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    await _subscriptionService.SubscribeAsync(_grpcClient, _currentUserId!.Value, _currentRoom, _subscriptionCts.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Subscription error: {ex.Message}");
                }
            });
        }

        private static void HandleGameEvent(GameEvent ev)
        {
            switch (ev.EventType)
            {
                case EventType.Info:
                    Console.WriteLine($"[INFO] {ev.Message}");
                    break;
                case EventType.PlayerJoined:
                    Console.WriteLine($"[JOINED] {ev.Message}");
                    break;
                case EventType.MoveMade:
                    Console.WriteLine($"[MOVE] {ev.Message}");
                    break;
                case EventType.GameEnded:
                    Console.WriteLine($"[END] {ev.Message}");
                    break;
                default:
                    Console.WriteLine($"[EVENT] {ev.EventType}: {ev.Message}");
                    break;
            }
        }

        private static bool TryParseMove(string input, out MoveType move)
        {
            switch (input.ToUpper())
            {
                case "K":
                    move = MoveType.Rock;
                    return true;
                case "N":
                    move = MoveType.Scissors;
                    return true;
                case "B":
                    move = MoveType.Paper;
                    return true;
                default:
                    throw new ArgumentException("Invalid move. Use K for Rock, N for Scissors, B for Paper.");
            }
        }
    }
