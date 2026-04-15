namespace BackgroundServices.Models;

public class CampaignRequest
{
    public string CampaignId { get; set; }
    public List<string> Emails { get; set; }
}
