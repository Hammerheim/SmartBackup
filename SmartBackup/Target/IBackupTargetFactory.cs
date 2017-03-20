using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Target
{
  public interface IBackupTargetFactory
  {
    void InitializeTarget(int id, long tail);
    IBackupTarget GetTarget(int id);
    IBackupTarget GetTarget(int id, bool createNewIfMissing);
  }
}
