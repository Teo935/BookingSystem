using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

// Service Layer: contratto della business logic su Room (validazione, orchestrazione
// del Repository). I metodi che possono fallire per una regola di business (non per un
// errore tecnico) ritornano una tupla (Success, Error, Value) invece di lanciare
// eccezioni — le eccezioni in questo progetto restano riservate a errori davvero
// eccezionali, non a validazioni previste come "prezzo non valido".
//
// Questa interfaccia ha anche una seconda implementazione oltre a RoomService:
// CachedRoomService (Infrastructure) la implementa come decorator per aggiungere la
// cache Redis senza toccare la business logic originale (pattern Decorator).
public interface IRoomService
{
    Task<(bool Success, string? Error, Room? Room)> CreateRoomAsync(CreateRoomRequest request);
    Task<List<Room>> GetAllRoomsAsync();
    Task<Room?> GetRoomAsync(int id);
    Task<Room?> UpdateRoomAsync(int id, UpdateRoomRequest request);
    Task<RoomDeleteResult> DeleteRoomAsync(int id);
}
