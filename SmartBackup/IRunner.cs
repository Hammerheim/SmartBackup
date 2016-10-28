using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public interface IRunner
  {
    Task<IFileLog> Run(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, Action<double> progressCallback);
  }
}
