using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class BackupTarget : IBackupTarget
  {
    private LinkedList<BackupTargetItem> content = new LinkedList<BackupTargetItem>();
    private Dictionary<string, BackupTargetItem> keySearchContent = new Dictionary<string, BackupTargetItem>();
    private long maxLengthInMegaBytes;
    private long tail;

    public BackupTarget(long maxLengthInMegaBytes)
    {
      this.maxLengthInMegaBytes = maxLengthInMegaBytes;
    }

    public long Tail => tail;

    public int TargetId { get; set; }

    public void AddFile(FileInformation file)
    {
      
    }

    public bool CanContain(FileInformation file)
    {
      return (tail + file.Size < maxLengthInMegaBytes);
    }

    public bool Contains(string key)
    {
      return (keySearchContent.ContainsKey(key));
    }

    public BackupTargetItem GetItem(string key)
    {
      if (Contains(key))
        return keySearchContent[key];
      return null;
    }
  }
}
