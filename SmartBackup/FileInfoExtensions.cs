using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public static class FileInfoExtensions
  {
    public static FileInfo GetRelativePath(this FileInfo rootPath, string fullPath)
    {
      var root = rootPath.FullName;
      if (!fullPath.EndsWith("\\"))
        fullPath = fullPath + "\\";
      if (!root.EndsWith("\\"))
        root = rootPath + "\\";
      var uri = new Uri(root);

      var result = uri.MakeRelativeUri(new Uri(fullPath)).ToString();
      result = WebUtility.UrlDecode(result.ToString());
      result = result.Replace('/', '\\');
      if (result.StartsWith("."))
        result = result.Substring(1);

      return new FileInfo(result);
    }

  }
}
