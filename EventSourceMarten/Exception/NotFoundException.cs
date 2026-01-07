namespace EventSourceMarten.Exception;

public class NotFoundException: System.Exception
{
    public NotFoundException(string driverNotFound) {
        throw new System.Exception(driverNotFound);
    }
}