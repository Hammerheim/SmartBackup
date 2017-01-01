using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class BackupTargetItem
  {
    public FileInformation File { get; set; }
    public long TargetOffset { get; set; }
    public long TargetLength { get; set; }
    public bool Compressed { get; set; }
  }
}
