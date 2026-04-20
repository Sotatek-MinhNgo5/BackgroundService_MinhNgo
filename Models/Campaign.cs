namespace BackgroundServices.Models;

public class Campaign
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int TotalEmails { get; set; }
    public string Status { get; set; } = "Draft"; // Draft | Running | Done
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();
}
