namespace EventSourceMarten.Contracts.Responses.Diver;
public record DiverResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string LicenseNumber { get; init; }

    public double? GeoLat { get; init; }
    public double? GeoLon { get; init; }
}