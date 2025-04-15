using System.ComponentModel.DataAnnotations;

namespace GenerateQRService.Models
{
    public class ConcatPdfDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Document link is not provided")]
        public string[] Links { get; set; }
    }
}
