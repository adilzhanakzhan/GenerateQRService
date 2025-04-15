namespace GenerateQRService.Models;

public class CertificateInfo
{
    public string? SerialNumber { get; set; }

    public string? SubjectName { get; set; }
    public string? SubjectSurname { get; set; }
    public string? SubjectOrgItem { get; set; }

    public string? Issuer { get; set; }
    public string? Thumbprint { get; set; }

    public string? IssueDate { get; set; }
    public string? ExpirationDate { get; set; }

    public string GetQRPayload()
    {
        var items = new string[]
        {
            $"Автор подписи: {SubjectSurname} {SubjectName}",
            $"Издатель: {Issuer}",
            $"Отпечаток: {Thumbprint}",
            $"Серийный номер: {SerialNumber}",
            $"Действителен с: {IssueDate}",
            $"Действителен по: {ExpirationDate}"
        };

        return string.Join("\n", items);
    }
}
