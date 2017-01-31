using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Progress;

namespace Vibe.Hammer.SmartBackup
{
  public class Runner : IRunner
  {
    private IFileInformationGatherer gatherer = new FileInformationGatherer(new MD5Hasher());
    private int currentFile;
    private int maxNumberOfFiles;
    private DateTime lastProgressReport;

    public async Task<bool> Backup(IFileLog log, DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback)
    {
      currentFile = 0;
      progressCallback.Report(new ProgressReport("Starting backup"));
      progressCallback.Report(new ProgressReport("Reading existing content catalogues if any..."));
      var contentCatalogue = new ContentCatalogue();
      await contentCatalogue.BuildFromExistingBackups(targetRoot, 1024);

      progressCallback.Report(new ProgressReport("Performing backup..."));
      lastProgressReport = DateTime.Now;
      maxNumberOfFiles = log.Files.Count();
      foreach (var file in log.Files)
      {
        if (DateTime.Now - lastProgressReport > TimeSpan.FromSeconds(5))
        {
          progressCallback.Report(new ProgressReport(file.FullyQualifiedFilename, currentFile, maxNumberOfFiles));
          await contentCatalogue.WriteCatalogue();
          lastProgressReport = DateTime.Now;
        }
        currentFile++;
        var currentVersion = contentCatalogue.GetNewestVersion(file);
        if (currentVersion == null)
          await contentCatalogue.InsertFile(file);
        else
        {
          if (currentVersion.SourceFileInfo.LastModified < file.LastModified)
          {
            currentVersion.Version += 1;
            await contentCatalogue.InsertFile(file);
          }
        }
      }
      await contentCatalogue.CloseTargets();
      progressCallback.Report(new ProgressReport("Backup complete", currentFile, currentFile));
      return true;
    }


    public async Task<IFileLog> DeepScan(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback)
    {
      return await Scan(sourceRoot, targetRoot, progressCallback, true);
    }

    public async Task<IFileLog> ShallowScan(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback)
    {
      return await Scan(sourceRoot, targetRoot, progressCallback, false);
    }

    private async Task<IFileLog> Scan(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback, bool deepScan)
    {
      var logger = new FileTreeLog();
      var recurser = new DirectoryRecurser();
      var result = await recurser.RecurseDirectory(sourceRoot, new SimpleFileHandler(new FileInformationGatherer(new MD5Hasher()), logger), false, progressCallback);
      if (result)
      {
        return logger;
      }
      return null;

    }

  }
}
