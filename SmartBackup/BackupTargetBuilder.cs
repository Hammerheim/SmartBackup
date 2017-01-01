using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class BackupTargetBuilder : IBackupTargetBuilder
  {
    private List<BackupTarget> targets = new List<BackupTarget>();

    public void Build(IFileLog log)
    {
      
    }
  }
}
