namespace EventSourceMarten.Contracts.Requests.Diver;
public record RenameDiverRequest
{
    public required string Name { get; init; }
}