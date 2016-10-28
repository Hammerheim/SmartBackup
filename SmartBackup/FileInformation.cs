using System;

namespace Vibe.Hammer.SmartBackup
{
  public class FileInformation
  {
    public int FilenameHash { get; set; }
    public string ContentHash { get; set; }
    public string Directory { get; set; }
    public string FileName { get; set; }
    public DateTimeOffset LastModified { get; set; }
  }
}