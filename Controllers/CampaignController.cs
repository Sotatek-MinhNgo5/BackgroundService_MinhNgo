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
    }
}