using EventSourceMarten.Contracts.Aggregates.Diver;
using EventSourceMarten.Entities;
using Marten.Events.Aggregation;

namespace EventSourceMarten.Projections;

public class DriverProjection: SingleStreamProjection<Driver, Guid>
{
    public static Driver Create(CreateDiver e)
        => new Driver {
                          Id = e.Id,
                          Name = e.Name,
                          LicenseNumber = e.LicenseNumber,
                          UpdatedAt = e.UpdatedAt,
                          GeoLat = e.GeoLat,
                          GeoLon = e.GeoLon
                      };

    public void Apply(RenameDiver e, Driver state) {

        state.Name = e.Name;
        state.UpdatedAt = e.UpdatedAt;
    }


    public void Apply(ChageLicenseNumberDiver e, Driver state){

        state.LicenseNumber = e.LicenseNumber;
        state.UpdatedAt = e.UpdatedAt;
    }
    
    public void Apply(ChageGeoLocationDiver e, Driver state){

        state.GeoLat = e.GeoLat;
        state.GeoLon = e.GeoLon;
        state.UpdatedAt = DateTime.UtcNow;
    }

    public void Apply(DeleteDiver e, Driver state){

        state.IsDeleted = true;
        state.UpdatedAt = e.UpdatedAt;
    }
}