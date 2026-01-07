using EventSourceMarten.Contracts.Requests.Diver;
using EventSourceMarten.Mapping;
using EventSourceMarten.Services.Driver;
using FastEndpoints;

namespace EventSourceMarten.Endpoints;
public class DriverCreateEndpoint: Endpoint<CreateDiverRequest, Guid>
{
    private readonly IDriverService _driverService;

    public DriverCreateEndpoint(IDriverService driverService) {
        _driverService = driverService;
    }

    public override async Task HandleAsync(CreateDiverRequest req, CancellationToken ct) {
        var driver = req.ToDriver();
        driver.Id = Guid.NewGuid();

       var driverId = await _driverService.CreateAsync(driver, ct: ct);

        await Send.OkAsync(
            driverId,
            cancellation: ct);
    }

    public override void Configure() {
        Post("/drivers");
        AllowAnonymous();

        Summary(s =>
            {
                s.Summary = "Создать водителя";
                s.Description =
                    "Создаёт нового водителя и возвращает его идентификатор (GUID).\n" +
                    "Возможные ответы:\n" +
                    "- 200: создан, возвращён Id\n" +
                    "- 400: некорректные данные запроса\n" +
                    "- 500: внутренняя ошибка сервера";
                s.ExampleRequest = new CreateDiverRequest
                                   {
                                       Name = "Иван Иванов",
                                       LicenseNumber = "AB123456"
                                   };
            });

        Description(b => b
                        .Produces<Guid>(200, "application/json")
                        .Produces(400)
                        .Produces(500)
        );
    }
}