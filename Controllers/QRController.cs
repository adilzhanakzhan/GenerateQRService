using Microsoft.AspNetCore.Mvc;

using GenerateQRService.Models;
using GenerateQRService.Services;

namespace GenerateQRService.Controllers;


[ApiController]
[Route("api/qr")]
public class QRController : ControllerBase
{
    private readonly ILogger<QRController> _logger;
    private readonly QRService _service;

    public QRController(ILogger<QRController> logger, QRService service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpGet("healthcheck")]
    public IActionResult HealthCheck()
    {
        _logger.LogInformation("{} Got request HealthCheck, service is working.", DateTime.Now.ToString("F"));
        return Ok("Service is working!");
    }

    [HttpPost("generate/old")]
    public async Task<IActionResult> GenerateQR([FromBody] InputPayload input)
    {
        if (input == null)
        {
            _logger.LogError("{} Got request GenerateQR, but input is empty!", DateTime.Now.ToString("F"));
            return BadRequest();
        }

        try
        {
            using var document = await _service.Download(input.DownloadLink!);
            //using var font = await _service.Download(input.DownloadFontLink!);

            _logger.LogInformation("{} Processing Signature information.", DateTime.Now.ToString("F"));

            var info = _service.DecodeSignerInformation(input);
            var qrPayload = info.GetQRPayload();

            using var qr = _service.CreateQR(qrPayload);
            using var result = _service.AddImageToPdf(input, qr, document);

            _logger.LogInformation("{} Adding QR code to the file.", DateTime.Now.ToString("F"));

            return Ok(Convert.ToBase64String(System.IO.File.ReadAllBytes(result.Path)));
        }
        catch (Exception ex)
        {
            _logger.LogError("The process failed: {}", ex.ToString());
            Console.WriteLine("The process failed: {0}", ex.ToString());

            throw;
        }
    }

    [HttpPost("generate")]
    public async Task<IActionResult> OutgoingCorrespondenceQR([FromBody] OutgoingCorrespondenceQRPayload payload)
    {
        if (payload == null)
        {
            _logger.LogError("{} Got request GenerateQR, but input is empty!", DateTime.Now.ToString("F"));
            return BadRequest();
        }

        try
        {
            using var document = await _service.Download(payload.DownloadLink!);
            using var result = _service.AddQRToOutgoingCorrespondence(payload, document);

            return Ok(Convert.ToBase64String(System.IO.File.ReadAllBytes(result.Path)));
        }
        catch (Exception ex)
        {
            _logger.LogError("The process failed: {}", ex.ToString());
            Console.WriteLine("The process failed: {0}", ex.ToString());

            throw;
        }
    }
}
