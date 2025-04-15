using QRCoder;

using System.Security.Cryptography.Pkcs;

using GenerateQRService.Models;
using GenerateQRService.Controllers;

using iText.IO.Font;
using iText.IO.Image;

using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Action;

using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Utils;


namespace GenerateQRService.Services;


public class QRService
{
    private readonly ILogger<QRController> _logger;
    private readonly HttpClient _client = new();

    private readonly string FONT = Directory.GetCurrentDirectory() + "/Fonts/Calibri.ttf";

    public QRService(ILogger<QRController> logger)
    {
        _logger = logger;
    }

    public async Task<FileReference> Download(string link)
    {
        var path = System.IO.Path.GetTempFileName();
        var response = await _client.SendAsync(new(HttpMethod.Get, link));

        using var content = await response.Content.ReadAsStreamAsync();

        return await FileReference.Create(path, content);
    }

    public CertificateInfo DecodeSignerInformation(InputPayload input)
    {
        SignedCms cms = new();

        cms.Decode(Convert.FromBase64String(input.Base64Signature!));

        var certificate = cms.Certificates[0];

        var attributes = certificate.Subject.Split(",")
            .Select(x => x.Trim().Split("="))
            .Select(entry => (Key: entry[0], Value: entry[1]))
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        return new CertificateInfo
        {
            SubjectName = attributes["G"],
            SubjectOrgItem = attributes.GetValueOrDefault("T"),
            SubjectSurname = attributes["CN"], // автор подписи

            Issuer = certificate.Issuer, // издатель
            Thumbprint = certificate.Thumbprint, // отпечаток
            SerialNumber = certificate.SerialNumber, // серийный номер

            IssueDate = certificate.NotBefore.ToShortDateString(), // действителен с
            ExpirationDate = certificate.NotAfter.ToShortDateString(), // действителен по                                    
        };
    }

    public FileReference CreateQR(string info)
    {
        using var qrGenerator = new QRCodeGenerator();

        var qrCodeData = qrGenerator.CreateQrCode(info, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);

        var path = System.IO.Path.GetTempFileName();

        File.WriteAllBytes(path, qrCode.GetGraphic(90));

        return new(path);
    }

    //public void AddImageToPdf(string qrPath, string qrEDSPath, string source, string target, InputPayload input)
    public FileReference AddImageToPdf(InputPayload input, FileReference qr, FileReference source)
    {
        const string text = $"Данный документ согласно пункту 1 статьи 7 ЗРК от 7 января 2003 года N370-II\n" +
            $"«Об электронном документе и электронной цифровой подписи»,\n" +
            $"удостоверенный посредством электронной цифровой подписи лица, имеющего полномочия на его подписание,\n" +
            $"равнозначен подписанному документу на бумажном носителе.\n" +
            $"Для проверки электронного документа загрузите CMS файл.\n" +
            $"Проверить CMS файл можно по ссылке ";

        var target = System.IO.Path.GetTempFileName();
        var font = PdfFontFactory.CreateFont(FONT, PdfEncodings.IDENTITY_H);

        using var pdf = new PdfDocument(new PdfReader(source.Path), new PdfWriter(target));
        using var document = new Document(pdf);

        var pageSize = PageSize.A4;
        var pageLeft = pageSize.GetLeft();
        var pageBottom = pageSize.GetBottom();

        var QR = new Image(ImageDataFactory.Create(File.ReadAllBytes(qr.Path)))
            .ScaleAbsolute(60, 60)
            .SetMargins(0, 0, 0, 0);


        for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
        {
            document.Add(QR.SetFixedPosition(i, pageLeft + 17, pageBottom + 15));

            //Дата: (Дата и время подписания). Копия электронного документа. Версия СЭД: ELMA 365. Положительный результат проверки ЭЦП
            var sideParagraph = new Paragraph(
                new Text(input.RightSideText ?? "")
                    .SetFont(font)
                    .SetFontSize(7));

            document.ShowTextAligned(sideParagraph, 570, pageBottom + 280, i, TextAlignment.CENTER, VerticalAlignment.MIDDLE, 1.5708f); //y=450

            //Исполнитель: Иванов И.И. тел. +7 (707)111 111
            var topParagraph = new Paragraph(
                new Text(input.InfoExecuter ?? "")
                    .SetFont(font)
                    .SetFontSize(8));

            document.ShowTextAligned(topParagraph, pageLeft + 20, pageBottom + 80, i, TextAlignment.LEFT, VerticalAlignment.MIDDLE, 0);//100

            //Create LINK
            PdfLinkAnnotation annotation = new PdfLinkAnnotation(new Rectangle(0, 0))
                .SetAction(PdfAction.CreateURI("https://ezsigner.kz/#!/checkCMS"));

            var link = new Link("https://ezsigner.kz/#!/checkCMS", annotation)
                .SetFont(font)
                .SetFontSize(8)
                .SetUnderline();

            // Adding link to paragraph 
            Paragraph content = new Paragraph(new Text(text).SetFont(font).SetFontSize(8))
                .Add(link);

            document.ShowTextAligned(content, pageLeft + 80, pageBottom + 45, i, TextAlignment.LEFT, VerticalAlignment.MIDDLE, 0);//55
        }

        return new(target);
    }

    public CertificateInfo DecodeOutgoingCorrespondenceCert(string signature)
    {
        SignedCms cms = new();

        cms.Decode(Convert.FromBase64String(signature));

        var certificate = cms.Certificates[0];

        var attributes = certificate.Subject.Split(",")
            .Select(x => x.Trim().Split("="))
            .Select(entry => (Key: entry[0], Value: entry[1]))
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        return new CertificateInfo
        {
            SubjectOrgItem = attributes.GetValueOrDefault("T"),
            SubjectSurname = attributes["CN"], // автор подписи

            Issuer = certificate.Issuer, // издатель
            Thumbprint = certificate.Thumbprint, // отпечаток
            SerialNumber = certificate.SerialNumber, // серийный номер

            IssueDate = certificate.NotBefore.ToShortDateString(), // действителен с
            ExpirationDate = certificate.NotAfter.ToShortDateString(), // действителен по                                    
        };
    }

    public FileReference ConcatFiles(FileReference[] links)
    {
        var target = System.IO.Path.GetTempFileName();
        var font = PdfFontFactory.CreateFont(FONT, PdfEncodings.IDENTITY_H);

        var pageSize = PageSize.A4;
        var pageBottom = pageSize.GetBottom();

        var source = links.First();
        using var pdf = new PdfDocument(new PdfWriter(target));
        using var document = new Document(pdf, pageSize, immediateFlush: true);

        var merger = new PdfMerger(pdf);

        foreach (var link in links )
        {
            using var reader = new PdfDocument(new PdfReader(link.Path));

            merger.Merge(reader, 1, reader.GetNumberOfPages());
        }

        //document.Flush();

        return new(target);
    }

    public FileReference AddQRToOutgoingCorrespondence(OutgoingCorrespondenceQRPayload payload, FileReference source)
    {
        var target = System.IO.Path.GetTempFileName();
        var font = PdfFontFactory.CreateFont(FONT, PdfEncodings.IDENTITY_H);

        var pageSize = PageSize.A4;
        var pageBottom = pageSize.GetBottom();

        using var pdf = new PdfDocument(new PdfReader(source.Path), new PdfWriter(target));
        using var document = new Document(pdf, pageSize, immediateFlush: false);

        using var appQRFile = CreateQR(payload.AppLink!);
        var appQR = new Image(ImageDataFactory.Create(File.ReadAllBytes(appQRFile.Path)))
            .ScaleAbsolute(50, 50)
            .SetMargins(0, 0, 0, 0);

        const string footerHeader = "Подписано электронной-цифровой подписью (ЭЦП)";

        for (int pageNumber = 1; pageNumber <= pdf.GetNumberOfPages(); pageNumber++)
        {
            document.ShowTextAligned( 
                new Paragraph(footerHeader).SetFont(font).SetFontSize(10),

                document.GetLeftMargin(),
                pageBottom + 56,
                pageNumber,
                TextAlignment.LEFT,
                VerticalAlignment.MIDDLE,
                0
            );

            document.ShowTextAligned( 
                new Paragraph($"Экземпляр процесса: {payload.ProcessId}").SetFont(font).SetFontSize(10),

                document.GetLeftMargin(),
                pageBottom + 44,
                pageNumber,

                TextAlignment.LEFT,
                VerticalAlignment.MIDDLE,
                0
            );

            document.ShowTextAligned( 
                new Paragraph($"Дата подписания: {payload.SignDate}").SetFont(font).SetFontSize(10),

                document.GetLeftMargin(),
                pageBottom + 32,
                pageNumber,

                TextAlignment.LEFT,
                VerticalAlignment.MIDDLE,
                0
            );

            document.ShowTextAligned( 
                new Paragraph($"Система электронного документооборота: ELMA {payload.Version}").SetFont(font).SetFontSize(10),

                document.GetLeftMargin(),
                pageBottom + 20,
                pageNumber,

                TextAlignment.LEFT,
                VerticalAlignment.MIDDLE,
                0
            );

            document.Add(appQR.SetFixedPosition(pageNumber, 4 * pageSize.GetWidth() / 6, 10));

            if (!string.IsNullOrEmpty(payload.FBNumber))
            {
                document.ShowTextAligned( 
                    new Paragraph(payload.FBNumber).SetFont(font).SetFontSize(10),

                    pageSize.GetRight() - document.GetRightMargin() - 40,
                    pageBottom + 40,
                    pageNumber,

                    TextAlignment.RIGHT,
                    VerticalAlignment.MIDDLE,
                    0
                );
            }
        }

        var page = pdf.AddNewPage();

        document.Add(new AreaBreak(AreaBreakType.LAST_PAGE));
         
        var table = new Table(UnitValue.CreatePercentArray(6))
            .SetWidth(pageSize.GetWidth() - (document.GetLeftMargin() + document.GetRightMargin()));

        Cell createHeader(string text) => new Cell().Add(new Paragraph(text).SetFont(font).SetBold().SetFontSize(11));
        Cell createCell(string text) => new Cell().Add(new Paragraph(text).SetFont(font).SetFontSize(11));

        table.AddHeaderCell(createHeader("ФИО подписанта"));
        table.AddHeaderCell(createHeader("Должность подписанта"));
        table.AddHeaderCell(createHeader("Владелец ЭЦП"));
        table.AddHeaderCell(createHeader("Дата и время подписи"));
        table.AddHeaderCell(createHeader("ЭЦП действительна"));
        table.AddHeaderCell(createHeader("Отпечаток сертификата"));

        foreach (var entry in payload.Signatures!)
        {
            var cert = DecodeOutgoingCorrespondenceCert(entry.Base64Signature!);

            using var thumbQRFile = CreateQR(cert.Thumbprint!);
            var thumbQR = new Image(ImageDataFactory.Create(File.ReadAllBytes(thumbQRFile.Path)))
                .ScaleAbsolute(80, 80)
                .SetMargins(0, 0, 0, 0);

            table.AddCell(createCell(entry.Executor!));
            table.AddCell(createCell(entry.ExecutorPosition!));

            table.AddCell(createCell(cert.SubjectSurname!));
            table.AddCell(createCell(entry.SignDate!));

            table.AddCell(createCell($"с {cert.IssueDate} по {cert.ExpirationDate}"));
            table.AddCell(new Cell().Add(thumbQR));
        }


        document.Add(
            new Paragraph(new Text("Документ подписан электронной-цифровой подписью:").SetBold().SetFontSize(11).SetFont(font).SetBold())
                .SetPageNumber(pdf.GetPageNumber(page))
        );

        document.Add(table);

        document.Add(
            new Paragraph(
                new Text(
                    "Осы құжат «Электрондық құжат және электрондық цифрлық қолтаңба туралы» " +
                    "Қазақстан Республикасының 2003 жылғы 7 қаңтардағы N 370-II Заңы 7 бабының 1 " +
                    "тармағына сәйкес қағаз тасығыштағы құжатпен бірдей."
                )
                .SetFont(font)
                .SetFontSize(9)
            )
            .SetPageNumber(pdf.GetPageNumber(page))
        );

        document.Add(
            new Paragraph(
                new Text(
                    "Данный документ согласно пункту 1 статьи 7 ЗРК от 7 января 2003 года N370-II " +
                    "«Об электронном документе и электронной цифровой подписи» равнозначен документу на бумажном носителе"
                )
                .SetFont(font)
                .SetFontSize(9)
            )
            .SetPageNumber(pdf.GetPageNumber(page))
        );


        document.Flush();

        return new(target);
    }
}
