using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using BookingSystem.Data;
using BookingSystem.Models;

namespace BookingSystem.Controllers;

[ApiController]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoomsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("api/rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        if (request.PricePerNight <= 0)
            return BadRequest("PricePerNight must be greater than zero.");

        var room = new Room
        {
            Name = request.Name,
            PricePerNight = request.PricePerNight
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
    }

    [HttpGet("api/rooms")]
    public async Task<IActionResult> GetAllRooms()
    {
        var rooms = await _db.Rooms.ToListAsync();
        return Ok(rooms);
    }

    [HttpGet("api/rooms/{id}")]
    public async Task<IActionResult> GetRoom(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();

        return Ok(room);
    }

    [HttpPut("api/rooms/{id}")]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();

        room.Name = request.Name;
        room.PricePerNight = request.PricePerNight;

        await _db.SaveChangesAsync();

        return Ok(room);
    }

    [HttpDelete("api/rooms/{id}")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();

        _db.Rooms.Remove(room);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "Cannot delete room: it has existing bookings." });
        }

        return NoContent();
    }
}