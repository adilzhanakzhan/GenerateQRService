namespace GenerateQRService.Models
{
    public class FileReference : IDisposable
    {
        public string Path { get; set; }

        public FileReference(string path)
        {
            Path = path;
        }

        public static async Task<FileReference> Create(string path, Stream content)
        {
            using var writer = new FileStream(path, FileMode.Create, FileAccess.Write);

            await content.CopyToAsync(writer);

            return new(path);
        }

        void IDisposable.Dispose() => File.Delete(Path);
    }
}
