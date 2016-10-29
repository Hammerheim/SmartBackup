using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  internal class DirectoryRecurser : IDirectoryRecurser
  {
    public async Task<bool> RecurseDirectory(DirectoryInfo root, IFileHandler fileHandler)
    {
      foreach (var file in root.GetFiles())
      {
        var result = await fileHandler.Handle(file);
        if (!result)
          return false;
      }

      foreach (var directory in root.GetDirectories())
      {
        var result = await RecurseDirectory(directory, fileHandler);
        if (!result)
          return false;
      }
      return true;
    }
  }
}
