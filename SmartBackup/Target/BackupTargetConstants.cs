using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Target
{
  internal class BackupTargetConstants
  {
    public static int ContentCatalogueOffset => 256 * 1024;
    public static int DataOffset => 3000 * 1024;    
  }
}
