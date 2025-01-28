using System.Net.Http.Json;

namespace Rps.Client.Services;

public class RestClient
{
    private readonly HttpClient _httpClient;

    public RestClient(string baseAddress)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseAddress)
        };
        _httpClient.DefaultRequestVersion = new Version(2, 0);
        _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
    }

    public async Task<string> CreateTransactionAsync(int fromUserId, int toUserId, decimal amount)
    {
        var body = new
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount
        };

        var response = await _httpClient.PostAsJsonAsync("/api/game/transactions", body);
        if (response.IsSuccessStatusCode)
        {
            return "Transaction completed.";
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Transaction failed: {error}");

    }

    public async Task<int> CreateMatchAsync(int userId, string roomName, decimal bet)
    {
        var body = new
        {
            UserId = userId,
            RoomName = roomName,
            Bet = bet
        };

        var response = await _httpClient.PostAsJsonAsync("/api/game/matches", body);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Create match failed: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<CommandResult>();
        return result.MatchId;
    }
}
