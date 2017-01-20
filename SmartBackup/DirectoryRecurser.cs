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
      return await InternalRecurser(root, root, fileHandler, deepScan);
    }

    private async Task<bool> InternalRecurser(DirectoryInfo currentRoot, DirectoryInfo originalRoot, IFileHandler fileHandler, bool deepScan)
    {
      foreach (var file in currentRoot.GetFiles())
      {
        var result = await fileHandler.Handle(file, originalRoot, deepScan);
        if (!result)
          return false;
      }

      foreach (var directory in currentRoot.GetDirectories())
      {

        try
        {
          var result = await InternalRecurser(directory, originalRoot, fileHandler, deepScan);
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
