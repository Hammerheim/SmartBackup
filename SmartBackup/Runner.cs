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
    private ContentCatalogue catalogue;
    private readonly DirectoryInfo root;

    public Runner(DirectoryInfo targetRoot)
    {
      if (targetRoot == null)
        throw new ArgumentNullException(nameof(targetRoot));
      catalogue = null;
      root = targetRoot;
    }
    public async Task<bool> Backup(IFileLog log, DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback)
    {
      currentFile = 0;
      progressCallback.Report(new ProgressReport("Starting backup"));
      progressCallback.Report(new ProgressReport("Reading existing content catalogues if any..."));
      await InitializeContentCatalogue(targetRoot, progressCallback);

      progressCallback.Report(new ProgressReport("Performing backup..."));
      lastProgressReport = DateTime.Now;
      maxNumberOfFiles = log.Files.Count();
      foreach (var file in log.Files)
      {
        currentFile++;
        await ReportProgress(progressCallback, file);
        var currentVersion = catalogue.GetNewestVersion(file);
        if (currentVersion == null)
          await catalogue.InsertFile(file);
        else
        {
          if (currentVersion.SourceFileInfo.LastModified < file.LastModified)
          {
            currentVersion.Version += 1;
            await catalogue.InsertFile(file);
          }
        }
      }
      await catalogue.CloseTargets();
      progressCallback.Report(new ProgressReport("Backup complete", currentFile, currentFile));
      return true;
    }

    private async Task ReportProgress(IProgress<ProgressReport> progressCallback, FileInformation file)
    {
      if (DateTime.Now - lastProgressReport > TimeSpan.FromSeconds(5))
      {
        progressCallback.Report(new ProgressReport(file.FullyQualifiedFilename, currentFile, maxNumberOfFiles));
        await catalogue.WriteCatalogue();
        lastProgressReport = DateTime.Now;
      }
    }

    public async Task<IFileLog> Scan(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback)
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

    public async Task<bool> CalculateMissingHashes(DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback)
    {
      await InitializeContentCatalogue(targetRoot, progressCallback);

      currentFile = 0;
      var allContentWithoutHashes = catalogue.GetAllContentEntriesWithoutHashes(progressCallback);
      if (!allContentWithoutHashes.Any())
      {
        progressCallback.Report(new ProgressReport("All files are already hashed."));
        return true;
      }
      maxNumberOfFiles = allContentWithoutHashes.Count();
      lastProgressReport = DateTime.Now;

      progressCallback.Report(new ProgressReport("Scanning for missing file hashes..."));
      foreach (var contentItem in allContentWithoutHashes)
      {
        var backupTarget = catalogue.GetBackupTargetFor(contentItem);
        await backupTarget.CalculateHashes(contentItem);

        currentFile++;
        await ReportProgress(progressCallback, contentItem.SourceFileInfo);
      }
      await catalogue.WriteCatalogue();
      return true;
    }

    public async Task<bool> ReplaceDublicatesWithLinks(DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback)
    {
      await InitializeContentCatalogue(targetRoot, progressCallback);

      currentFile = 0;
      var allPossibleDublicates = catalogue.GetAllPossibleDublicates(progressCallback);
      if (!allPossibleDublicates.Any())
      {
        progressCallback.Report(new ProgressReport("There are no dublicates in the backup."));
        return true;
      }
      maxNumberOfFiles = allPossibleDublicates.Count();
      lastProgressReport = DateTime.Now;

      progressCallback.Report(new ProgressReport("Linking dublicate files..."));

      foreach (var entryList in allPossibleDublicates)
      {
        currentFile++;
        ContentCatalogueBinaryEntry primaryEntry = null;
        foreach (var entry in entryList)
        {
          if (primaryEntry == null)
          {
            primaryEntry = entry;
            continue;
          }

          var link = new ContentCatalogueLinkEntry(entry, primaryEntry);
          catalogue.ReplaceBinaryEntryWithLink(entry, link);

          await ReportProgress(progressCallback, link.SourceFileInfo);
        }
      }
      await catalogue.WriteCatalogue();
      return true;
    }

    private async Task InitializeContentCatalogue(DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback)
    {
      if (catalogue == null)
      {
        progressCallback.Report(new ProgressReport("Reading content catalogue..."));
        catalogue = new ContentCatalogue(1024, targetRoot);
        await catalogue.BuildFromExistingBackups(targetRoot, 1024);
      }
    }

    public Task<bool> DefragmentBinaries(DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback)
    {
      throw new NotImplementedException();
    }
  }
}
