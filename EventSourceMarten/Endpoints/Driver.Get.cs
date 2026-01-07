using EventSourceMarten.Contracts.Responses.Diver;
using EventSourceMarten.Mapping;
using EventSourceMarten.Services.Driver;
using FastEndpoints;

namespace EventSourceMarten.Endpoints;
public class DriverGetEndpoint: EndpointWithoutRequest
{
    private readonly IDriverService _driverService;

    public DriverGetEndpoint(IDriverService driverService) {
        _driverService = driverService;
    }

    public override async Task HandleAsync( CancellationToken ct) {
        var id = Route<Guid>("id");

        var existingDriver = await _driverService.GetAsync(id, ct: ct);

        if (existingDriver is null) {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        await Send.OkAsync(existingDriver.ToDriverResponse(), cancellation: ct);
    }

    public override void Configure() {
        Get("/drivers/{id}");
        AllowAnonymous();
        Summary(s =>
            {
                s.Summary = "Получить водителя по id \n" +
                            "- 200: возвращён водитель\n" +
                            "- 400: некорректные данные запроса\n" +
                            "- 500: внутренняя ошибка сервера";
                s.Description = "Получает (не удалённого) водителя. Возвращает поля сущности.";
            });

        Description(b => b
                        .Produces<DiverResponse>(200, "application/json")
                        .Produces(404)
                        .Produces(400)
                        .Produces(500)
        );
    }
}