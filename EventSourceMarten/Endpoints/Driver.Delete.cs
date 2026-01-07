using EventSourceMarten.Exception;
using EventSourceMarten.Services.Driver;
using FastEndpoints;

namespace EventSourceMarten.Endpoints;
public class DriverDeleteEndpoint: EndpointWithoutRequest
{
    private readonly IDriverService _driverService;

    public DriverDeleteEndpoint(IDriverService driverService) {
        _driverService = driverService;
    }

    public override async Task HandleAsync(CancellationToken ct) {
        var id = Route<Guid>("id");
        var existingDriver = await _driverService.GetAsync(id, ct: ct) ??
                             throw new NotFoundException("Driver not found");

        bool isDeleted = await _driverService.Delete(existingDriver, ct);

        if (!isDeleted) {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        await Send.OkAsync(isDeleted, cancellation: ct).ConfigureAwait(false);
    }

    public override void Configure() {
        Delete("/drivers/{id}");
        AllowAnonymous();
        Summary(s =>
            {
                s.Summary = "Удалить водителя по id";
                s.Description = "Удаляет (или помечает как удалённого) водителя. Возвращает true при успехе.";
            });

        Description(b => b
                        .Produces<bool>(200, "application/json")
                        .Produces(404)
                        .Produces(400)
                        .Produces(500)
        );
    }
}