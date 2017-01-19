using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class FillOneBackupTargetSelectionStrategy : BackupTargetStrategyBase
  {
    public FillOneBackupTargetSelectionStrategy(int maxSizeinMegaBytes)
      : base(maxSizeinMegaBytes)
    {

    }

    public override IBackupTarget GetTargetWithRoom(List<IBackupTarget> targets, FileInformation fileInformation)
    {
      if (targets == null)
        return null;
      if (fileInformation == null)
        throw new ArgumentNullException(nameof(fileInformation));

      foreach (var target in targets)
      {
        if (target.CanContain(fileInformation))
          return target;
      }
      return null;
    }
  }
}
