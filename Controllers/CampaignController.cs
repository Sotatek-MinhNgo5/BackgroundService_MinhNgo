using BackgroundServices.Data;
using BackgroundServices.Models;
using BackgroundServices.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackgroundServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampaignController : ControllerBase
    {
        private readonly IRabbitMqService _mqService;

        public CampaignController(IRabbitMqService mqService)
        {
            _mqService = mqService;
        }

        [HttpPost("send-campaign")]
        public async Task<IActionResult> SendCampaign(
            [FromBody] CampaignRequest request,
            [FromServices] AppDb db)
        {
            if (request.Emails?.Any() != true)
                return BadRequest("Email list cannot be empty.");

            var campaign = new Campaign
            {
                Name = string.IsNullOrWhiteSpace(request.Name) ? "Unnamed Campaign" : request.Name,
                Subject = request.Subject,
                TotalEmails = request.Emails.Count,
                Status = "Running",
                CreatedAt = DateTime.UtcNow
            };

            db.Campaigns.Add(campaign);
            await db.SaveChangesAsync(); 

            foreach (var email in request.Emails)
            {
                db.EmailLogs.Add(new EmailLog
                {
                    CampaignId = campaign.Id,
                    Email = email,
                    Status = "Pending"
                });

                await _mqService.PublishAsync(new EmailMessage
                {
                    To = email,
                    CampaignId = campaign.Id
                });
            }

            await db.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Queued {request.Emails.Count} emails!",
                CampaignId = campaign.Id,
                CampaignName = campaign.Name
            });
        }

        [HttpGet("{campaignId:int}/status")]
        public IActionResult GetStatus(int campaignId, [FromServices] AppDb db)
        {
            var campaign = db.Campaigns.Find(campaignId);
            if (campaign == null) return NotFound("Campaign not found.");

            var stats = db.EmailLogs
                .Where(x => x.CampaignId == campaignId)
                .GroupBy(x => x.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            var total = stats.Sum(x => x.Count);
            var sent = stats.FirstOrDefault(x => x.Status == "Sent")?.Count ?? 0;
            var pending = stats.FirstOrDefault(x => x.Status == "Pending")?.Count ?? 0;
            var failed = stats.FirstOrDefault(x => x.Status == "Failed")?.Count ?? 0;

            return Ok(new
            {
                CampaignId = campaignId,
                CampaignName = campaign.Name,
                CampaignStatus = campaign.Status,
                total,
                sent,
                pending,
                failed,
                percent = total == 0 ? 0 : Math.Round((double)sent * 100 / total, 2)
            });
        }

        [HttpGet("{campaignId:int}/emails")]
        public IActionResult GetEmails(
            int campaignId,
            [FromServices] AppDb db,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] string? status = null)
        {
            if (pageSize > 1000) pageSize = 1000;

            var query = db.EmailLogs.Where(x => x.CampaignId == campaignId);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(x => x.Status == status);

            var total = query.Count();

            var data = query
                .OrderBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new { x.Email, x.Status, x.RetryCount, x.SentAt })
                .ToList();

            return Ok(new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                data
            });
        }

        [HttpPost("{campaignId:int}/retry")]
        public async Task<IActionResult> Retry(
            int campaignId,
            [FromServices] AppDb db)
        {
            var campaign = db.Campaigns.Find(campaignId);
            if (campaign == null) return NotFound("Campaign not found.");

            var failedEmails = db.EmailLogs
                .Where(x => x.CampaignId == campaignId && x.Status == "Failed")
                .ToList();

            if (!failedEmails.Any())
                return Ok("No failed emails to retry.");

            foreach (var item in failedEmails)
            {
                item.Status = "Pending";
                item.RetryCount = 0;

                await _mqService.PublishAsync(new EmailMessage
                {
                    To = item.Email,
                    CampaignId = campaignId
                });
            }

            campaign.Status = "Running";
            await db.SaveChangesAsync();

            return Ok(new { Message = $"Retrying {failedEmails.Count} emails.", CampaignId = campaignId });
        }

        [HttpGet("list")]
        public IActionResult GetCampaigns([FromServices] AppDb db)
        {
            var campaigns = db.Campaigns
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.Name, x.Subject, x.Status, x.TotalEmails, x.CreatedAt })
                .ToList();

            return Ok(campaigns);
        }
    }
}