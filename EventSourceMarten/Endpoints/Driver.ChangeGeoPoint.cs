using EventSourceMarten.Contracts.Requests.Diver;
using EventSourceMarten.Contracts.Responses.Diver;
using EventSourceMarten.DTO;
using EventSourceMarten.Entities;
using EventSourceMarten.Exception;
using EventSourceMarten.Mapping;
using EventSourceMarten.Services.Driver;
using FastEndpoints;

namespace EventSourceMarten.Endpoints;
public class ChageGeoLocationDiverEndpoint: Endpoint<ChangeGeoLocationDiverRequest, DiverResponse>
{
    private readonly IDriverService _driverService;

    public ChageGeoLocationDiverEndpoint(IDriverService driverService) {
        _driverService = driverService;
    }

    public override async Task HandleAsync(ChangeGeoLocationDiverRequest req, CancellationToken ct) {
        var id = Route<Guid>("id");

        var existingDriver = await _driverService.GetAsync(id, ct: ct)
                          ?? throw new NotFoundException("Driver not found");

        var newDriver = new Driver {
                                       Id = id,
                                       Name = existingDriver.Name,
                                       GeoLat = req.Location.Lat,
                                       GeoLon = req.Location.Lon,
                                       LicenseNumber = existingDriver.LicenseNumber,

                                   };


        await _driverService.UpdateAsync(newDriver, ct: ct);

        var driverResponse = newDriver.ToDriverResponse();

        await Send.OkAsync(
            driverResponse,
            cancellation: ct);
    }

    public override void Configure() {
        Put("/drivers/{id}/change-geo-location");
        AllowAnonymous();

        Summary(s =>
            {
                s.Summary = "Изменить геолокацию водителя";
                s.Description =
                    "Обновляет географическое местоположение (широту и долготу) для существующего водителя.\n" +
                    "Ответы:\n" +
                    "- 200: геолокация успешно обновлена\n" +
                    "- 400: некорректные данные запроса\n" +
                    "- 404: водитель с указанным Id не найден\n" +
                    "- 500: внутренняя ошибка сервера";

                s.ExampleRequest =
                    new ChangeGeoLocationDiverRequest { Location = new GeoPointDto { Lat = 59.9386, Lon = 30.3141 } };
            });

        Description(b => b
                        .Produces<DiverResponse>(200, "application/json")
                        .Produces(400)
                        .Produces(404)
                        .Produces(500)
        );
    }
}