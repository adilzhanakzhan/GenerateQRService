using Microsoft.AspNetCore.Mvc;

using GenerateQRService.Models;
using GenerateQRService.Services;

namespace GenerateQRService.Controllers;


[ApiController]
[Route("api/concat")]
public class ConcatController : ControllerBase
{
    private readonly ILogger<QRController> _logger;
    private readonly QRService _service;

    public ConcatController(ILogger<QRController> logger, QRService service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpPost("pdf")]
    public async Task<IActionResult> ConcatPDFs([FromBody] ConcatPdfDto payload)
    {
        if (payload.Links.Length == 0)
        {
            throw new BadHttpRequestException("No files provided");
        }

        try
        {
            var files = await Task.WhenAll(payload.Links.Select(link => _service.Download(link)));

            using var result = _service.ConcatFiles(files);

            foreach (var file in files)
            {
                (file as IDisposable).Dispose();
            }

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
