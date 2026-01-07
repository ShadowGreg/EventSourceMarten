using EventSourceMarten.DTO;

namespace EventSourceMarten.Contracts.Requests.Diver;
public record ChangeGeoLocationDiverRequest
{
    public required GeoPointDto Location { get; init; }
}