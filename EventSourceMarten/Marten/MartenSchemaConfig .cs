using EventSourceMarten.Entities;
using Marten;

namespace EventSourceMarten.Marten;
public static class MartenSchemaConfig
{
    public static void ConfigureEventSourceSchema(this StoreOptions opts)
    {
        opts.Schema.For<DriverHistoryItem>()
            .Duplicate(x => x.DriverId)
            .Duplicate(x => x.At)
            .Duplicate(x => x.MessageRu)
            .Duplicate(x => x.MessageEn)
            ;
        
        opts.Schema.For<Driver>()
            .Duplicate(x => x.Name)
            .Duplicate(x => x.LicenseNumber)
            .Duplicate(x => x.UpdatedAt)
            .Duplicate(x => x.IsDeleted)
            ;
    }
}