namespace BackgroundServices.Models;

public class EmailLog
{
    public int Id { get; set; }

    public int CampaignId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending | Sent | Failed
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }

    public Campaign? Campaign { get; set; }
}