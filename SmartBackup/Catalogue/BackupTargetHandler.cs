using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Target;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  public class BackupTargetHandler : IBackupTargetHandler
  {
    protected int ExpectedFileSize { get; set; }
    protected string FilenamePattern { get; set; }
    protected string BackupDirectoryName { get; set; }
    public BackupTargetHandler(int expectedFileSize, string filenamePattern, string backupDirectory)
    {
      ExpectedFileSize = expectedFileSize;
      FilenamePattern = filenamePattern;
      BackupDirectoryName = backupDirectory;
    }

    private IBackupTargetFactory backupTargetFactory = null;
    protected IBackupTargetFactory TargetFactory
    {
      get
      {
        if (backupTargetFactory == null)
          backupTargetFactory = new BackupTargetFactory(ExpectedFileSize, new DirectoryInfo(BackupDirectoryName), FilenamePattern);
        return backupTargetFactory;
      }
    }

    public virtual IBackupTarget GetTarget(int id) => GetTarget(id, false);

    public virtual IBackupTarget GetTarget(int id, bool allowCreation) => TargetFactory.GetTarget(id, allowCreation);

    public virtual void InitializeTargets(List<TargetContentCatalogue> catalogueTargets) => catalogueTargets.ForEach(target => TargetFactory.InitializeTarget(target.BackupTargetIndex, target.CalculateTail()));

    public virtual void CloseTargets() => TargetFactory.CloseAll();

    public virtual IBackupTarget GetBackupTargetContainingFile(IContentCatalogue catalogue, FileInformation file)
    {
      var target = catalogue.Targets.FirstOrDefault(t => t.KeySearchContent.ContainsKey(file.FullyQualifiedFilename));
      return target == null ? null : GetTarget(target.BackupTargetIndex);
    }

    public virtual IBackupTarget GetBackupTargetFor(IContentCatalogue catalogue, ContentCatalogueEntry entry)
    {
      foreach (var target in catalogue.Targets)
      {
        if (target.KeySearchContent.ContainsKey(entry.Key))
        {
          if (target.KeySearchContent[entry.Key].FirstOrDefault(e => e.Key == entry.Key && e.Version == entry.Version) != null)
            return GetTarget(target.BackupTargetIndex);
        }
      }

      return null;
    }
  }
}
