namespace EventSourceMarten.Contracts.Aggregates;
public class Base
{
    public Guid Id { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public double? GeoLat { get; set; }
    public double? GeoLon { get; set; } 
    
    public DateTimeOffset UpdatedAt { get; set; } = DateTime.UtcNow;
}