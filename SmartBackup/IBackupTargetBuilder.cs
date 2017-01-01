using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public interface IBackupTargetBuilder
  {
    void Build(IFileLog log);
  }
}
