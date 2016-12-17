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
    public async Task<bool> RecurseDirectory(DirectoryInfo root, IFileHandler fileHandler, bool deepScan)
    {
      foreach (var file in root.GetFiles())
      {
        var result = await fileHandler.Handle(file, deepScan);
        if (!result)
          return false;
      }

      foreach (var directory in root.GetDirectories())
      {

        try
        {
          var result = await RecurseDirectory(directory, fileHandler, deepScan);
          if (!result)
            return false;
        }
        catch (Exception)
        {

          throw;
        }
      }
      return true;
    }
  }
}
