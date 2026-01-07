namespace EventSourceMarten.Contracts.Requests.Diver;
public record ChageLicenseNumberDiverRequest
{
    public required string LicenseNumber { get; init; }
}