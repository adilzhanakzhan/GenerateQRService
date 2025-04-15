using System.ComponentModel.DataAnnotations;

namespace GenerateQRService.Models;

public class InputPayload
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Document link is not provided")]
    public string? DownloadLink { get; set; }
    public string? DownloadFontLink { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Signature is not provided")]
    public string? Base64Signature { get; set; }

    public string? RightSideText { get; set; }
    public string? InfoExecuter { get; set; }

    public string? DocID { get; set; }
    public string? ViewLink { get; set; }
}

public class OutgoingCorrespondenceQRPayload
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Document link is not provided")]
    public string? DownloadLink { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Sign date is not provided")]
    public string? SignDate { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Process ID is not provided")]
    public string? ProcessId { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "App link is not provided")]
    public string? AppLink { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Version is not provided")]
    public string? Version { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Signatures are not provided")]
    public List<SignatureInfo>? Signatures { get; set; }

    public string? FBNumber { get; set; }
}

public class SignatureInfo
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Signature is not provided")]
    public string? Base64Signature { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Executor name is not provided")]
    public string? Executor { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Executor position is not provided")]
    public string? ExecutorPosition { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Sign date is not provided")]
    public string? SignDate { get; set; }

}


