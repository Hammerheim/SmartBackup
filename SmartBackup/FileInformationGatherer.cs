using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  internal class FileInformationGatherer : IFileInformationGatherer
  {
    private IHasher hasher;

    public FileInformationGatherer(IHasher hasher)
    {
      this.hasher = hasher;
    }
    public FileInformation Gather(FileInfo file, DirectoryInfo root, bool deepScan)
    {
      var info = new FileInformation
      {
        Directory = file.Directory.FullName,
        FileName = file.Name,
        LastModified = file.LastWriteTime,
        FullyQualifiedFilename = file.FullName,
        Size = file.Length,
        RelativePath = GetRelativePath(file.Directory.FullName, root.FullName)
      };
      return info;
      
    }

    private string GetRelativePath(string fullPath, string rootPath)
    {
      if (!fullPath.EndsWith("\\"))
        fullPath = fullPath + "\\";
      if (!rootPath.EndsWith("\\"))
        rootPath = rootPath + "\\";
      var uri = new Uri(rootPath);

      var result = uri.MakeRelativeUri(new Uri(fullPath)).ToString();
      result = WebUtility.UrlDecode(result.ToString());
      result = result.Replace('/', '\\');
      if (result.StartsWith("."))
        result = result.Substring(1);
      
      return result;
    }
  }
}
