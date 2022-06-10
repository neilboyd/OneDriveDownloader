using OneDriveUtils;

var url = args.Length > 0 ? args[0] : throw new ArgumentException("Specify sharing link as argument");
var filename = string.Empty;
var extension = string.Empty;
using var stream = await Downloader.GetStreamAsync(url, (n, x) => { filename = n; extension = x; });
using var fileStream = File.Create(filename + extension);
stream.CopyTo(fileStream);

Console.WriteLine("Downloaded to {0}", fileStream.Name);
