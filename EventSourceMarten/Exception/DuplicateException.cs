namespace EventSourceMarten.Services.Driver;
public class DuplicateException: System.Exception
{
    public DuplicateException(string? driverAlreadyExists = null) {
        throw new System.Exception($"Driver already exists{driverAlreadyExists}");
    }
}