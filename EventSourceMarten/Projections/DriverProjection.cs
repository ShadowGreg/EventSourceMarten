using System.Runtime.InteropServices.ComTypes;
using EventSourceMarten.Contracts.Aggregates;
using EventSourceMarten.Contracts.Aggregates.Diver;
using EventSourceMarten.Contracts.Requests;
using EventSourceMarten.Contracts.Requests.Diver;
using EventSourceMarten.Entities;
using Marten;
using Marten.Events.Aggregation;

namespace EventSourceMarten.Projections;

public class DriverProjection: SingleStreamProjection<Driver, Guid>
{
    public static Driver Create(CreateDiver e)
        => new Driver {
                          Id = e.Id,
                          Name = e.Name,
                          LicenseNumber = e.LicenseNumber,
                          UpdatedAt = e.UpdatedAt
                      };

    public void Apply(RenameDiver e, Driver state) {

        state.Name = e.Name;
        state.UpdatedAt = DateTime.UtcNow;
    }


    public void Apply(ChageLicenseNumberDiver e, Driver state){

        state.LicenseNumber = e.LicenseNumber;
        state.UpdatedAt = DateTime.UtcNow;
    }

    public void Apply(DeleteDiver e, Driver state){

        state.IsDeleted = true;
        state.UpdatedAt = e.UpdatedAt;
    }
}