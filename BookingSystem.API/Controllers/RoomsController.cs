using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;

namespace BookingSystem.API.Controllers;

// Livello API della Clean Architecture: il Controller riceve la richiesta HTTP,
// chiama il Service e traduce l'esito in una risposta HTTP. Nessuna business logic
// qui dentro — nota che IRoomService viene iniettato per interfaccia (Dependency
// Injection): a runtime, grazie alla registrazione in Program.cs, l'istanza reale
// ricevuta è CachedRoomService (il decorator con cache Redis), ma il Controller non
// lo sa e non deve saperlo.
[ApiController]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    // Authorization basata su ruoli: solo un utente con il ruolo "Admin" nel JWT può
    // creare/modificare/cancellare stanze. Le GET restano pubbliche (nessun [Authorize]).
    [HttpPost("api/rooms")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var (success, error, room) = await _roomService.CreateRoomAsync(request);

        if (!success)
        {
            return BadRequest(error);
        }

        return CreatedAtAction(nameof(GetRoom), new { id = room!.Id }, room);
    }

    [HttpGet("api/rooms")]
    public async Task<IActionResult> GetAllRooms()
    {
        var rooms = await _roomService.GetAllRoomsAsync();
        return Ok(rooms);
    }

    [HttpGet("api/rooms/{id}")]
    public async Task<IActionResult> GetRoom(int id)
    {
        var room = await _roomService.GetRoomAsync(id);
        if (room == null) return NotFound();

        return Ok(room);
    }

    [HttpPut("api/rooms/{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
    {
        var room = await _roomService.UpdateRoomAsync(id, request);
        if (room == null) return NotFound();

        return Ok(room);
    }

    [HttpDelete("api/rooms/{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var result = await _roomService.DeleteRoomAsync(id);

        return result switch
        {
            RoomDeleteResult.NotFound => NotFound(),
            RoomDeleteResult.Conflict => Conflict(new { error = "Cannot delete room: it has existing bookings." }),
            _ => NoContent()
        };
    }
}
