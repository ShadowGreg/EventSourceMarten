namespace EventSourceMarten.Contracts.Aggregates.Diver;
public class CreateDiver: Base
{
    public required string Name { get; init; }
    public required string LicenseNumber { get; init; }
}