using System;

namespace Vibe.Hammer.SmartBackup
{
  public class FileInformation
  {
    public FileInformation()
    {

    }

    public FileInformation(string content)
      : this()
    {
      var contentSections = content.Split(',');
      Directory = contentSections[0];
      FileName = contentSections[1];
      Size = long.Parse(contentSections[2]);
      LastModified = DateTime.Parse(contentSections[3]);
      FilenameHash = int.Parse(contentSections[4]);
      ContentHash = contentSections[5];
    }

    public int FilenameHash { get; set; }
    public string ContentHash { get; set; }
    public string Directory { get; set; }
    public string FileName { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public long Size { get; set; }

    public override string ToString()
    {
      return $"{Directory},{FileName},{Size},{LastModified.ToString("D")},{FilenameHash},{ContentHash}";
    }
  }
}