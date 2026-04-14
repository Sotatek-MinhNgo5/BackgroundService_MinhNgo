namespace BackgroundServices.Models;

public class OrderRequest
{
    public string OrderId { get; set; } = Guid.NewGuid().ToString();
    public string CustomerEmail { get; set; } = string.Empty;
}