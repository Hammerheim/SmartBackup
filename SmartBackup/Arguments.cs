using System.IO;

namespace Vibe.Hammer.SmartBackup
{
  public class Arguments
  {
    public ProgramAction Actions { get; set; }
    public int FileSizeInMB { get; internal set; } = 1024;
    public bool PrintHelp { get; internal set; }
    public DirectoryInfo Source { get; internal set; }
    public DirectoryInfo Target { get; internal set; }

    public bool ShouldBackup
    {
      get { return (Actions & ProgramAction.Backup) == ProgramAction.Backup; }
    }

    public bool ShouldMaintain
    {
      get { return (Actions & ProgramAction.Maintenance) == ProgramAction.Maintenance; }
    }

    public bool ShouldExtract
    {
      get { return (Actions & ProgramAction.Extraction) == ProgramAction.Extraction; }
    }

    public bool Debug { get; internal set; }
    public string FilenamePattern { get; internal set; }
    public bool Compress { get; internal set; }
  }
}