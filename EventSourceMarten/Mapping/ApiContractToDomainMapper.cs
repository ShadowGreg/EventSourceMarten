using EventSourceMarten.Contracts.Requests.Diver;
using EventSourceMarten.Contracts.Responses.Diver;
using EventSourceMarten.Entities;

namespace EventSourceMarten.Mapping;
public static class ApiContractToDomainMapper
{
    public static Driver ToDriver(this CreateDiverRequest request) {
        return new Driver {
                              Id = Guid.NewGuid(),
                              Name = request.Name,
                              LicenseNumber = request.LicenseNumber,
                              GeoLat = request.Location.Lat,
                              GeoLon = request.Location.Lon
                          };
    }

    public static DiverResponse ToDriverResponse(this Driver request) {
        return new DiverResponse {
                                     Id = request.Id,
                                     Name = request.Name,
                                     LicenseNumber = request.LicenseNumber,
                                     GeoLat = request.GeoLat,
                                     GeoLon = request.GeoLon
                                 };
    }
}