using EventSourceMarten.Contracts.Requests.Diver;
using EventSourceMarten.Contracts.Responses.Diver;
using EventSourceMarten.Entities;
using EventSourceMarten.Exception;
using EventSourceMarten.Mapping;
using EventSourceMarten.Services.Driver;
using FastEndpoints;

namespace EventSourceMarten.Endpoints;
public class RenameDiverEndpoint: Endpoint<RenameDiverRequest, DiverResponse>
{
    private readonly IDriverService _driverService;

    public RenameDiverEndpoint(IDriverService driverService) {
        _driverService = driverService;
    }

    public override async Task HandleAsync(RenameDiverRequest req, CancellationToken ct) {
        var id = Route<Guid>("id");

        var existingDriver = await _driverService.GetAsync(id, ct: ct)
                          ?? throw new NotFoundException("Driver not found");

        var newDriver = new Driver {
                                       Id = id,
                                       Name = req.Name,
                                       LicenseNumber = existingDriver.LicenseNumber, 
                                       GeoLat = existingDriver.GeoLat,
                                       GeoLon = existingDriver.GeoLon
                                   };


        await _driverService.UpdateAsync(newDriver, ct: ct);

        var driverResponse = newDriver.ToDriverResponse();

        await Send.OkAsync(
            driverResponse,
            cancellation: ct);
    }

    public override void Configure() {
        Put("/drivers/{id}/rename");
        AllowAnonymous();

        Summary(s =>
            {
                s.Summary = "Переименовать водителя";
                s.Description =
                    "Изменяет имя существующего водителя по его идентификатору.\n" +
                    "Ответы:\n" +
                    "- 200: имя успешно изменено\n" +
                    "- 400: некорректные данные запроса\n" +
                    "- 404: водитель с указанным Id не найден\n" +
                    "- 500: внутренняя ошибка сервера";
                s.ExampleRequest = new RenameDiverRequest { Name = "Пётр Петров" };
            });

        Description(b => b
                        .Produces<DiverResponse>(200, "application/json")
                        .Produces(400)
                        .Produces(404)
                        .Produces(500)
        );
    }
}