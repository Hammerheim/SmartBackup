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
    public bool Debug { get; internal set; }
    public string FilenamePattern { get; internal set; }
    public bool Compress { get; internal set; }
    public bool ValidationOnExtraction { get; set; }
    public string IgnoreExtensions { get; set; }

    // Helpers
    public bool ShouldBackup => (Actions & ProgramAction.Backup) == ProgramAction.Backup;
    public bool ShouldMaintain => (Actions & ProgramAction.Maintenance) == ProgramAction.Maintenance;
    public bool ShouldExtract => (Actions & ProgramAction.Extraction) == ProgramAction.Extraction;
    public string[] IgnoredExtensions => IgnoreExtensions.Split(new[] { '|' });

  }
}