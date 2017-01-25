using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public abstract class BackupTargetStrategyBase : IBackupTargetSelectionStrategy
  {
    private int maxSizeInMegaBytes;

    protected long maxSizeInBytes => maxSizeInBytes * 1024 * 1024;

    public BackupTargetStrategyBase(int maxSizeInMegaBytes)
    {
      if (maxSizeInMegaBytes < 2)
        throw new ArgumentException(nameof(maxSizeInMegaBytes));

      this.maxSizeInMegaBytes = maxSizeInMegaBytes;
    }

    public abstract IBackupTarget GetTargetWithRoom(List<IBackupTarget> targets, FileInformation fileInformation);
  }
}
