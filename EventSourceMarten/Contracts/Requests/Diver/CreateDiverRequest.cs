namespace EventSourceMarten.Contracts.Requests.Diver;
public record CreateDiverRequest
{
    public required string Name { get; init; } 
    public required string LicenseNumber { get; init; }
}