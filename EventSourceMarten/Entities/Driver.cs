using EventSourceMarten.Contracts.Aggregates;
using EventSourceMarten.Contracts.Aggregates.Diver;

namespace EventSourceMarten.Entities;
public class Driver
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string LicenseNumber { get; set; } = null!;
    public bool IsDeleted { get; set; } = false;

    public DateTimeOffset UpdatedAt { get; set; } = DateTime.UtcNow;

    public Driver() { }
}