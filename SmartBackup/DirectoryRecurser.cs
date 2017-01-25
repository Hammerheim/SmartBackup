using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Progress;

namespace Vibe.Hammer.SmartBackup
{
  internal class DirectoryRecurser : IDirectoryRecurser
  {
    private int numberOfFiles = 0;
    private DateTime reportCheck;
    private int currentFile = 0;

    public async Task<bool> RecurseDirectory(DirectoryInfo root, IFileHandler fileHandler, bool deepScan, IProgress<ProgressReport> progressCallback)
    {
      numberOfFiles = 0;
      currentFile = 0;
      reportCheck = DateTime.Now;
      return await InternalRecurser(root, root, fileHandler, deepScan, progressCallback);
    }

    private async Task<bool> InternalRecurser(DirectoryInfo currentRoot, DirectoryInfo originalRoot, IFileHandler fileHandler, bool deepScan, IProgress<ProgressReport> progressCallback)
    {
      foreach (var file in currentRoot.GetFiles())
      {
        var result = await fileHandler.Handle(file, originalRoot, deepScan);
        if (DateTime.Now - reportCheck > TimeSpan.FromSeconds(1))
        {
          progressCallback.Report(new ProgressReport(file.FullName, currentFile, numberOfFiles));
          reportCheck = DateTime.Now;
        }
        currentFile++;

        if (!result)
          return false;
      }

      foreach (var directory in currentRoot.GetDirectories())
      {

        try
        {
          var result = await InternalRecurser(directory, originalRoot, fileHandler, deepScan, progressCallback);
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

    private void CalculateNumberOfFiles(DirectoryInfo root)
    {
      numberOfFiles += root.GetFiles().Length;
      foreach (var directory in root.GetDirectories())
        CalculateNumberOfFiles(directory);
    }
  }
}
