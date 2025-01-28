using Rps.Client.Services;

Console.WriteLine("Welcome to Rock-Paper-Scissors Game!");

var grpcClient = new GrpcClient("https://localhost:7299");
var restClient = new RestClient("https://localhost:7299");
var subscriptionService = new SubscriptionService();

var commandHandler = new CommandHandler(grpcClient, restClient, subscriptionService);

commandHandler.PrintHelp();

while (true)
{
    Console.Write("\n> ");
    var input = Console.ReadLine();
    if (input == null)
    {
        continue;
    }

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    await commandHandler.HandleCommandAsync(input);
}

Console.WriteLine("Goodbye!");
