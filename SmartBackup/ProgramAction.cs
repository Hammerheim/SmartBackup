using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public enum ProgramAction
  {
    Undefined = 0,
    Backup = 1,
    Maintenance = 2,
    Extraction = 4
  }
}
