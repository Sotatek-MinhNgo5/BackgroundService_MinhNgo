namespace BackgroundServices.Models;

public class EmailLog
{
    public int Id { get; set; }
    public string CampaignId { get; set; }
    public string Email { get; set; }
    public string Status { get; set; }
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
}