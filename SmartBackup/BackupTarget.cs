using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class BackupTarget
  {
    public int TargetId { get; set; }
    public long Tail { get; set; }
    public List<BackupTargetItem> ContentList { get; set; }
  }
}
