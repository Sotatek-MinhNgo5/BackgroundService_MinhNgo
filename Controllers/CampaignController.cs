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

            var campaignId = int.TryParse(request.CampaignId, out var id)
                ? id.ToString()
                : "0";

            foreach (var email in request.Emails)
            {
                var log = new EmailLog
                {
                    CampaignId = campaignId,
                    Email = email,
                    Status = "Pending"
                };

                db.EmailLogs.Add(log);

                await _mqService.PublishAsync(new EmailMessage
                {
                    To = email,
                    CampaignId = campaignId
                });
            }

            await db.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Queued {request.Emails.Count} emails!",
                CampaignId = campaignId
            });
        }

        [HttpGet("{campaignId}/status")]
        public IActionResult GetStatus(string campaignId, [FromServices] AppDb db)
        {
            var logs = db.EmailLogs.Where(x => x.CampaignId == campaignId);

            var total = logs.Count();
            var sent = logs.Count(x => x.Status == "Sent");
            var pending = logs.Count(x => x.Status == "Pending");
            var failed = logs.Count(x => x.Status == "Failed");

            return Ok(new
            {
                total,
                sent,
                pending,
                failed,
                percent = total == 0 ? 0 : (sent * 100 / total)
            });
        }

        [HttpGet("{campaignId}/emails")]
        public IActionResult GetEmails(string campaignId, [FromServices] AppDb db)
        {
            var data = db.EmailLogs
                .Where(x => x.CampaignId == campaignId)
                .Select(x => new
                {
                    x.Email,
                    x.Status,
                    x.RetryCount,
                    x.SentAt
                })
                .ToList();

            return Ok(data);
        }

        [HttpPost("{campaignId}/retry")]
        public async Task<IActionResult> Retry(string campaignId,
        [FromServices] AppDb db)
        {
            var failedEmails = db.EmailLogs
                .Where(x => x.CampaignId == campaignId && x.Status == "Failed")
                .ToList();

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

            await db.SaveChangesAsync();

            return Ok($"Retry {failedEmails.Count} emails");
        }
    }
}