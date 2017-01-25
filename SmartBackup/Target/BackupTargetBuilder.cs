using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class BackupTargetBuilder : IBackupTargetBuilder
  {
    private List<IBackupTarget> targets = new List<IBackupTarget>();
    private IBackupTargetSelectionStrategy selectionStrategy;

    public BackupTargetBuilder(IBackupTargetSelectionStrategy selectionStrategy)
    {
      this.selectionStrategy = selectionStrategy;
    }

    public void Build(IFileLog log)
    {
      foreach (var file in log.Files)
      {
        if (targets.Contains(file.FullyQualifiedFilename))
        {
          // Handle existing file
        }
        else
        {
          // New file
          var target = selectionStrategy.GetTargetWithRoom(targets, file);
          if (targets == null)
            target = CreateNewTarget();
        }
      }
    }

    private IBackupTarget CreateNewTarget()
    {
      throw new NotImplementedException();
    }
  }
}
