using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Progress;
using Vibe.Hammer.SmartBackup.Target;

namespace Vibe.Hammer.SmartBackup
{
  public class Runner : IRunner
  {
    private IFileInformationGatherer gatherer = new FileInformationGatherer();
    private int currentFile;
    private int maxNumberOfFiles;
    private DateTime lastProgressReport;
    private IContentCatalogue catalogue;
    private IExtractableContentCatalogue extractableCatalogue;
    private IBackupTargetHandler targetHandler;
    private readonly DirectoryInfo root;

    public Runner(DirectoryInfo targetRoot)
    {
      if (targetRoot == null)
        throw new ArgumentNullException(nameof(targetRoot));
      catalogue = null;
      root = targetRoot;
    }
    public async Task<bool> Backup(IFileLog log, DirectoryInfo targetRoot, int fileSize, string filenamePattern, bool compressIfPossible, IProgress<ProgressReport> progressCallback)
    {
      var errors = new List<FileInformation>();

      progressCallback.Report(new ProgressReport("Starting backup"));
      progressCallback.Report(new ProgressReport("Reading existing content catalogues if any..."));
      await InitializeContentCatalogue(targetRoot, fileSize, filenamePattern, progressCallback);

      progressCallback.Report(new ProgressReport("Performing backup..."));
      await ExecuteBackup(log, fileSize, targetRoot, filenamePattern, compressIfPossible, progressCallback, errors);

      if (errors.Any())
      {
        progressCallback.Report(new ProgressReport($"Encountered errors in {errors.Count()} files. Retrying"));
        log = new FileTreeLog(new List<string>());
        foreach (var item in errors)
        {
          log.Add(item);
        }
        errors = new List<FileInformation>();
        await ExecuteBackup(log, fileSize, targetRoot, filenamePattern, compressIfPossible, progressCallback, errors);
        if (errors.Any())
        {
          foreach (var item in errors)
          {
            progressCallback.Report(new ProgressReport($"Unable to backup file {item.FullyQualifiedFilename}"));
          }
        }
      }

      catalogue.WriteCatalogue();
      catalogue.Close(targetHandler);
      await Task.Delay(500);
      progressCallback.Report(new ProgressReport("Backup complete", currentFile, currentFile));
      return true;
    }

    private async Task ExecuteBackup(IFileLog log, int fileSize, DirectoryInfo targetRoot, string filenamePattern, bool compressIfPossible, IProgress<ProgressReport> progressCallback, List<FileInformation> errors)
    {
      currentFile = 0;
      lastProgressReport = DateTime.Now;
      maxNumberOfFiles = log.Files.Count();
      foreach (var file in log.Files)
      {
        currentFile++;
        ReportProgress(progressCallback, file);
        var currentVersion = extractableCatalogue.GetNewestVersion(file);

        if (currentVersion == null)
        {
          var target = GetOrCreateBackupTarget(fileSize, targetRoot, filenamePattern, file);
          await AddFileToTargetAndCatalogue(compressIfPossible, errors, file, target, 1);
        }
        else
        {
          if (currentVersion.SourceFileInfo.LastModified < file.LastModified)
          {
            var target = GetOrCreateBackupTarget(fileSize, targetRoot, filenamePattern, file);
            await AddFileToTargetAndCatalogue(compressIfPossible, errors, file, target, currentVersion.Version + 1);
          }
          else if (currentVersion.SourceFileInfo.LastModified == file.LastModified && currentVersion.Deleted)
          {
            // The deleted file has likely been restored, but there is a slight posibility that it is not the same file. Therefore the file is reinserted. 
            // If the binaries are the same, a maintenance run will convert one of them to a link and reclaim the space. This is the safe option rather than 
            // just setting the currentVersion.Deleted = false.
            var target = GetOrCreateBackupTarget(fileSize, targetRoot, filenamePattern, file);
            await AddFileToTargetAndCatalogue(compressIfPossible, errors, file, target, currentVersion.Version + 1);
          }
        }
      }
    }

    private IBackupTarget GetOrCreateBackupTarget(int maxFileSize, DirectoryInfo targetRoot, string filenamePattern, FileInformation file)
    {
      var findTargetResult = catalogue.TryFindBackupTargetWithRoom(file.Size);

      if (!findTargetResult.Found)
      {
        return targetHandler.GetTarget(catalogue.AddBackupTarget(), true);
      }
      else
      {
        return targetHandler.GetTarget(findTargetResult.TargetId);
      }
    }

    private async Task AddFileToTargetAndCatalogue(bool compressIfPossible, List<FileInformation> errors, FileInformation file, IBackupTarget target, int version)
    {
      var catalogueItem = await target.AddFile(file, version, compressIfPossible);
      if (catalogueItem != null)
      {
        catalogue.AddItem(target.TargetId, catalogueItem);
        var (verificationOriginalExists, verificationSucceeded) = await target.VerifyContent(catalogueItem);
        catalogueItem.Verified = verificationSucceeded;
        if (verificationOriginalExists && !verificationSucceeded)
        {
          catalogue.RemoveItem(catalogueItem);
          errors.Add(file);
        }
      }
      else
      {
        errors.Add(file);
      }
    }

    public async Task<bool> IdentifyDeletedFiles(DirectoryInfo targetRoot, int fileSize, string filenamePattern, IProgress<ProgressReport> progressCallback)
    {
      currentFile = 0;
      var numberOfDeletedFilesFound = 0;
      progressCallback.Report(new ProgressReport("Starting run to look for deleted files..."));
      progressCallback.Report(new ProgressReport("Reading existing content catalogues if any..."));
      await InitializeContentCatalogue(targetRoot, fileSize, filenamePattern, progressCallback);

      progressCallback.Report(new ProgressReport("Tombstoning deleted files..."));
      lastProgressReport = DateTime.Now;

      var entries = catalogue.EnumerateContent();
      maxNumberOfFiles = entries.Count();
      foreach (var entry in entries)
      {
        currentFile++;
        if (!entry.Deleted)
        {
          if (!File.Exists(entry.SourceFileInfo.FullyQualifiedFilename))
          {
            entry.Deleted = true;
            numberOfDeletedFilesFound++;
          }
        }
        ReportProgress(progressCallback, $"Found {numberOfDeletedFilesFound} deleted file(s)");
      }
      catalogue.Close(targetHandler);
      catalogue = null;
      progressCallback.Report(new ProgressReport("Backup complete", currentFile, currentFile));
      await Task.Delay(500);
      return true;
    }

    private void ReportProgress(IProgress<ProgressReport> progressCallback, FileInformation file)
    {
      if (DateTime.Now - lastProgressReport > TimeSpan.FromSeconds(5))
      {
        progressCallback.Report(new ProgressReport(file.FullyQualifiedFilename, currentFile, maxNumberOfFiles));
        catalogue.WriteCatalogue();
        lastProgressReport = DateTime.Now;
      }
    }

    private void ReportProgress(IProgress<ProgressReport> progressCallback, string message)
    {
      if (DateTime.Now - lastProgressReport > TimeSpan.FromSeconds(5))
      {
        progressCallback.Report(new ProgressReport(message, currentFile, maxNumberOfFiles));
        catalogue.WriteCatalogue();
        lastProgressReport = DateTime.Now;
      }
    }
    public async Task<IFileLog> Scan(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, List<string> ignoredExtensions, IProgress<ProgressReport> progressCallback)
    {
      var logger = new FileTreeLog(ignoredExtensions);
      var recurser = new DirectoryRecurser();
      var result = await recurser.RecurseDirectory(sourceRoot, new SimpleFileHandler(new FileInformationGatherer(), logger), false, progressCallback);
      if (result)
      {
        return logger;
      }
      return null;

    }

    public async Task<bool> CalculateMissingHashes(DirectoryInfo targetRoot, int fileSize, string filenamePattern, IProgress<ProgressReport> progressCallback)
    {
      await InitializeContentCatalogue(targetRoot, fileSize, filenamePattern, progressCallback);

      currentFile = 0;
      lastProgressReport = DateTime.Now;
      maxNumberOfFiles = catalogue.CountTotalEntries();
      progressCallback.Report(new ProgressReport("Scanning for missing file hashes..."));
      foreach (var entry in catalogue.EnumerateContent())
      {
        currentFile++;
        var binaryEntry = entry as ContentCatalogueBinaryEntry;
        if (binaryEntry == null)
          continue;
        if (!binaryEntry.Verified)
        {
          var backupTarget = targetHandler.GetBackupTargetFor(catalogue, entry);
          if (backupTarget != null)
          {
            ReportProgress(progressCallback, $"Verifying: {binaryEntry.SourceFileInfo.FileName}");

            var (verificationOriginalExists, verificationSucceeded) = await backupTarget.VerifyContent(binaryEntry);
            binaryEntry.Verified = verificationSucceeded;
            if (verificationOriginalExists && !verificationSucceeded)
            {
              catalogue.RemoveItem(binaryEntry);
              progressCallback.Report(new ProgressReport($"Critical error verifying file {binaryEntry.SourceFileInfo.FullyQualifiedFilename}. Binary content differ from original"));
            }
          }
        }
      }
      currentFile++;
      ReportProgress(progressCallback, $"Verification complete");
      catalogue.WriteCatalogue();
      catalogue.Close(targetHandler);
      catalogue = null;
      await Task.Delay(500);
      return true;
    }

    public async Task<bool> ReplaceDublicatesWithLinks(DirectoryInfo targetRoot, int fileSize, string filenamePattern, IProgress<ProgressReport> progressCallback)
    {
      await InitializeContentCatalogue(targetRoot, fileSize, filenamePattern, progressCallback);

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
        ContentCatalogueBinaryEntry primaryEntry = entryList.FirstOrDefault(x => x.Verified);
        if (primaryEntry == null)
        {
          continue;
        }
        foreach (var entry in entryList)
        {
          if (primaryEntry.SourceFileInfo.FullyQualifiedFilename == entry.SourceFileInfo.FullyQualifiedFilename)
            continue;

          var link = new ContentCatalogueUnclaimedLinkEntry(entry, primaryEntry);
          catalogue.ReplaceBinaryEntryWithLink(entry, link);
        }
        ReportProgress(progressCallback, primaryEntry.SourceFileInfo);
      }
      catalogue.WriteCatalogue();
      catalogue.Close(targetHandler);
      catalogue = null;
      await Task.Delay(500);
      return true;
    }

    private async Task InitializeContentCatalogue(DirectoryInfo targetRoot, int fileSize, string filenamePattern, IProgress<ProgressReport> progressCallback)
    {
      if (catalogue == null)
      {
        progressCallback.Report(new ProgressReport("Reading content catalogue..."));
        targetHandler = new BackupTargetHandler(fileSize, filenamePattern, targetRoot.FullName);
        catalogue = await ContentCatalogue.Build(targetRoot, fileSize, filenamePattern, targetHandler);
        extractableCatalogue = new ExtractableContentCatalogue(catalogue);
      }
    }

    public async Task<bool> DefragmentBinaries(DirectoryInfo targetRoot, int fileSize, string filenamePattern, IProgress<ProgressReport> progressCallback)
    {
      await InitializeContentCatalogue(targetRoot, fileSize, filenamePattern, progressCallback);

      currentFile = 0;
      maxNumberOfFiles = catalogue.CountUnclaimedLinks();
      if (maxNumberOfFiles == 0)
      {
        progressCallback.Report(new ProgressReport("There are no space that must be reclaimed."));
        return true;
      }
      lastProgressReport = DateTime.Now;

      progressCallback.Report(new ProgressReport("Reclaiming space..."));
       
      foreach (var target in catalogue.Targets)
      {
        currentFile++;
        var binaryTarget = targetHandler.GetTarget(target.BackupTargetIndex);
        var clonedConent = target.CloneContent();
        if (await binaryTarget.Defragment(clonedConent, progressCallback))
        {
          target.PromoteClonedContent(clonedConent);
          catalogue.ConvertAllUnclaimedLinksToClaimedLinks(target.BackupTargetIndex);
          catalogue.WriteCatalogue();
        }
      }
      catalogue.WriteCatalogue();
      return true;
    }
  }
}
