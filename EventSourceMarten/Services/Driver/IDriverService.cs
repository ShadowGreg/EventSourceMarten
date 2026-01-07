using DriverEntity = EventSourceMarten.Entities.Driver;

namespace EventSourceMarten.Services.Driver;
public interface IDriverService
{
    Task<Guid> CreateAsync(DriverEntity driver, CancellationToken ct = default);

    Task<DriverEntity?> GetAsync(Guid id, CancellationToken ct);

    Task<DriverEntity?> UpdateAsync(DriverEntity driver, CancellationToken ct = default);

    Task<bool> Delete(DriverEntity request, CancellationToken ct);
}