using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using Microsoft.Extensions.Options;

namespace BookingSystem.Infrastructure.Caching;

// Decorator Pattern: implementa IRoomService avvolgendo un'altra IRoomService (quella
// vera, RoomService) invece di ereditarne il comportamento. Aggiunge la cache Redis
// "da fuori", senza toccare una sola riga della business logic originale — rispetta
// l'Open/Closed Principle (aperto a nuove funzionalità come questa, chiuso a modifiche
// del codice esistente e già testato). In Program.cs, IRoomService viene registrato
// puntando a questa classe, che riceve RoomService come sua dipendenza "_inner".
//
// Cache solo su GetAllRoomsAsync (le liste sono l'operazione più costosa e più
// richiesta); GetRoomAsync per singolo id passa dritto, non vale la pena cachearlo
// (query per chiave primaria, già economica — vedi anche la nota su GetRoomAsync sotto).
public class CachedRoomService : IRoomService
{
    private const string AllRoomsCacheKey = "rooms:all";

    private readonly IRoomService _inner;
    private readonly ICacheService _cache;
    private readonly TimeSpan _roomsCacheDuration;

    public CachedRoomService(IRoomService inner, ICacheService cache, IOptions<CacheSettings> cacheSettings)
    {
        _inner = inner;
        _cache = cache;
        _roomsCacheDuration = TimeSpan.FromSeconds(cacheSettings.Value.RoomsCacheDurationSeconds);
    }

    // Pattern cache-aside: prima si controlla Redis (Cache Hit → ritorno immediato,
    // nessuna query SQL); se assente (Cache Miss) si legge dal database vero tramite
    // _inner e si ripopola la cache per le richieste successive, con scadenza
    // (RoomsCacheDurationSeconds, default 60s) per non servire dati stantii per sempre.
    public async Task<List<Room>> GetAllRoomsAsync()
    {
        var cached = await _cache.GetAsync<List<Room>>(AllRoomsCacheKey);
        if (cached != null)
            return cached;

        var rooms = await _inner.GetAllRoomsAsync();
        await _cache.SetAsync(AllRoomsCacheKey, rooms, _roomsCacheDuration);
        return rooms;
    }

    // Cache Invalidation: la chiave va rimossa solo se l'operazione è realmente andata a
    // buon fine (result.Success), altrimenti una richiesta fallita cancellerebbe
    // comunque una cache valida senza motivo.
    public async Task<(bool Success, string? Error, Room? Room)> CreateRoomAsync(CreateRoomRequest request)
    {
        var result = await _inner.CreateRoomAsync(request);
        if (result.Success)
            await _cache.RemoveAsync(AllRoomsCacheKey);

        return result;
    }

    public async Task<Room?> UpdateRoomAsync(int id, UpdateRoomRequest request)
    {
        var result = await _inner.UpdateRoomAsync(id, request);
        if (result != null)
            await _cache.RemoveAsync(AllRoomsCacheKey);

        return result;
    }

    public async Task<RoomDeleteResult> DeleteRoomAsync(int id)
    {
        var result = await _inner.DeleteRoomAsync(id);
        if (result == RoomDeleteResult.Success)
            await _cache.RemoveAsync(AllRoomsCacheKey);

        return result;
    }

    // Non cachato: lookup per chiave primaria, già indicizzato e a basso costo — Redis
    // aggiungerebbe una famiglia di chiavi extra da invalidare senza risolvere un vero
    // collo di bottiglia.
    public Task<Room?> GetRoomAsync(int id)
    {
        return _inner.GetRoomAsync(id);
    }
}
