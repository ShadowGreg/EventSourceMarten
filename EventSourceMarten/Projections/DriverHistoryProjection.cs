using EventSourceMarten.Contracts.Aggregates.Diver;
using EventSourceMarten.Entities;
using Marten.Events.Projections;

namespace EventSourceMarten.Projections;
public class DriverHistoryProjection: EventProjection
{
    public DriverHistoryProjection() {
        Project<CreateDiver>((e, ops) =>
            ops.Store(new DriverHistoryItem {
                                                Id = Guid.NewGuid(),
                                                DriverId = e.Id,
                                                At = e.UpdatedAt,
                                                MessageRu = $"Создан водитель. Имя: {e.Name}, права: {e.LicenseNumber}"
                                            }));

        Project<RenameDiver>((e, ops) =>
            ops.Store(new DriverHistoryItem {
                                                Id = Guid.NewGuid(),
                                                DriverId = e.Id,
                                                At = e.UpdatedAt,
                                                MessageRu = $"Изменено имя водителя на: {e.Name}"
                                            }));

        Project<ChageLicenseNumberDiver>((e, ops) =>
            ops.Store(new DriverHistoryItem {
                                                Id = Guid.NewGuid(),
                                                DriverId = e.Id,
                                                At = e.UpdatedAt,
                                                MessageRu = $"Изменён номер прав на: {e.LicenseNumber}"
                                            }));

        Project<DeleteDiver>((e, ops) =>
            ops.Store(new DriverHistoryItem {
                                                Id = Guid.NewGuid(),
                                                DriverId = e.Id,
                                                At = e.UpdatedAt,
                                                MessageRu = $"Водитель удалён {e.Name}, права {e.LicenseNumber}"
                                            }));
    }
}