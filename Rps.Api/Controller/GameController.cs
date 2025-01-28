using Microsoft.AspNetCore.Mvc;
using Rps.Api.Models;
using Rps.Application.Interfaces;

namespace Rps.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly ITransactionService _txService;

    public GameController(IGameService gameService, ITransactionService txService)
    {
        _gameService = gameService;
        _txService = txService;
    }

    [HttpPost("matches")]
    public async Task<IActionResult> CreateMatch([FromBody] CreateMatchModel model)
    {
        try
        {
            var matchId = await _gameService.CreateMatchAsync(model.UserId, model.Bet, model.RoomName);
            return Ok(new
            {
                Message = "Match created",
                MatchId = matchId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("transactions")]
    public async Task<IActionResult> DoTransaction([FromBody] TransactionModel model)
    {
        try
        {
            await _txService.CreateTransactionAsync(model.FromUserId, model.ToUserId, model.Amount);
            return Ok(new { Message = "Transaction done" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
