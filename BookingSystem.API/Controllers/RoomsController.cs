using Microsoft.AspNetCore.Mvc;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;

namespace BookingSystem.API.Controllers;

[ApiController]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpPost("api/rooms")]
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
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
    {
        var room = await _roomService.UpdateRoomAsync(id, request);
        if (room == null) return NotFound();

        return Ok(room);
    }

    [HttpDelete("api/rooms/{id}")]
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
