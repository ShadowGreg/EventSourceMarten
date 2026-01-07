namespace EventSourceMarten.Entities;
public class Driver
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string LicenseNumber { get; set; } = null!;
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset UpdatedAt { get; set; } = DateTime.UtcNow;
    public double? GeoLat { get; set; }
    public double? GeoLon { get; set; }
    public Driver() { }
}