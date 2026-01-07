namespace EventSourceMarten.Entities;

public class Shipment 
{
    public Guid Id { get; set; }
    public string Origin { get; set; } = null!;
    public string Destination { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string Status { get; set; } = null!;
    public Guid? AssignedDriverId { get; set; }
}