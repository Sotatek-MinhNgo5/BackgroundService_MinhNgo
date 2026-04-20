namespace BackgroundServices.Models;

public class CampaignRequest
{
    public string Name { get; set; } = string.Empty;    
    public string Subject { get; set; } = string.Empty;
    public List<string> Emails { get; set; } = new();
}
