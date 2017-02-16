using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Target
{
  internal class BackupTargetConstants
  {
    public static int DataOffset => 0;
    public static int BufferSize => 1000000;
    public static long MegaByte => (long)Math.Pow(2, 20);
  }
}
