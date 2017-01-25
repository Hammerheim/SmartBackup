using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public static class BackupTargetExtensions
  {
    public static bool Contains(this List<IBackupTarget> targets, string key)
    {
      foreach (var target in targets)
      {
        if (target.Contains(key))
          return true;
      }
      return false;
    }

    public static bool GetBackupTargetWithRoom(this List<IBackupTarget> targets, string key)
    {
      foreach (var target in targets)
      {
        if (target.Contains(key))
          return true;
      }
      return false;

    }

  }
}
