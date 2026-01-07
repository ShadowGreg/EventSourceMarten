namespace EventSourceMarten.Entities;
public class DriverHistoryItem
{
    public Guid Id { get; set; }              
    public Guid DriverId { get; set; }        
    public DateTimeOffset At { get; set; }    
    public string MessageRu { get; set; } = ""; 
    public string MessageEn { get; set; } = "";
}