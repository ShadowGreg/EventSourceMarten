using EventSourceMarten.Contracts.Aggregates.Diver;
using EventSourceMarten.Exception;
using Marten;
using DriverEntity = EventSourceMarten.Entities.Driver;

namespace EventSourceMarten.Services.Driver;
public class DriverService(IDocumentSession session): IDriverService
{
    public async Task<Guid> CreateAsync(DriverEntity driver,
                                        CancellationToken ct) {

        var existingDriver = await session.Query<DriverEntity>()
                                          .Where(x => x.LicenseNumber == driver.LicenseNumber &&
                                                      x.IsDeleted == false &&
                                                      x.Name == driver.Name)
                                          .SingleOrDefaultAsync(ct);
        if (existingDriver != null) {

            return existingDriver.Id;
        }


        var @event = new CreateDiver() {
                                           Id = driver.Id,
                                           Name = driver.Name,
                                           LicenseNumber = driver.LicenseNumber,
                                           UpdatedAt = driver.UpdatedAt
                                       };

        session.Events.StartStream<DriverEntity>(driver.Id, @event);

        await session.SaveChangesAsync(ct);
        return driver.Id;
    }

    public async Task<DriverEntity?> GetAsync(Guid id, CancellationToken ct) {
        var existingDriver = await session.Query<DriverEntity>()
                                          .Where(x => x.Id == id && x.IsDeleted == false)
                                          .SingleOrDefaultAsync(ct);
        return existingDriver;
    }

    public async Task<DriverEntity?> UpdateAsync(DriverEntity driver, CancellationToken ct) {
        var existingDriver = await session.LoadAsync<DriverEntity>(driver.Id, ct) ??
                             throw new NotFoundException("Driver not found");

        if (!String.Equals(existingDriver.Name, driver.Name, StringComparison.OrdinalIgnoreCase)) {
            session.Events.Append(driver.Id,
                new RenameDiver {
                                    Id = driver.Id,
                                    Name = driver.Name,
                                    LicenseNumber = existingDriver.LicenseNumber,
                                    UpdatedAt = driver.UpdatedAt
                                });
        }
        else if (!String.Equals(existingDriver.LicenseNumber,
                     driver.LicenseNumber,
                     StringComparison.OrdinalIgnoreCase)) {
            session.Events.Append(driver.Id,
                new ChageLicenseNumberDiver {
                                                Id = driver.Id,
                                                LicenseNumber = driver.LicenseNumber,
                                                Name = existingDriver.Name,
                                                UpdatedAt = driver.UpdatedAt
                                            });
        }

        await session.SaveChangesAsync(ct);
        return await session.LoadAsync<DriverEntity>(driver.Id, ct);
    }

    public async Task<bool> Delete(DriverEntity request, CancellationToken ct) {
        if (request.Id == Guid.Empty)
            return false;
        
        session.Events.Append(request.Id, new DeleteDiver {
                                                              Id = request.Id,
                                                              Name = request.Name,
                                                              LicenseNumber = request.LicenseNumber
                                                          });
        session.Delete<DriverEntity>(request.Id);
        await session.SaveChangesAsync(ct);
        return true;
    }
}