using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Compression;

namespace Vibe.Hammer.SmartBackup.Target
{
  public static class BackupTargetFactory
  {
    private static Dictionary<int, IBackupTarget> targets = new Dictionary<int, IBackupTarget>();
    private static int filesize;
    private static DirectoryInfo targetDirectory;
    private static string pattern;

    public static void InitializeTarget(int id, long tail, int fileSizeInMB, DirectoryInfo backupDirectory, string filenamePattern)
    {
      filesize = fileSizeInMB;
      targetDirectory = backupDirectory;
      pattern = filenamePattern;
      GetOrCreate(id, tail);
    }
    public static IBackupTarget CreateTarget(int id, long tail, int fileSizeInMB, DirectoryInfo backupDirectory, string filenamePattern)
    {
      filesize = fileSizeInMB;
      targetDirectory = backupDirectory;
      pattern = filenamePattern;

      return GetOrCreate(id, tail);
    }

    private static IBackupTarget GetOrCreate(int id, long tail)
    {
      if (IsCached(id))
        return targets[id];

      var target = new BackupTarget(new Sha256Hasher(), new CompressionHandler());
      target.Initialize(filesize, targetDirectory, id, tail, pattern);
      targets.Add(id, target);
      return target;
    }

    public static IBackupTarget GetCachedTarget(int id)
    {
      if (IsCached(id))
        return targets[id];
      return null;
    }

    private static bool IsCached(int backupTargetIndex)
    {
      return targets.ContainsKey(backupTargetIndex);
    }

    private static void ClearCache()
    {
      targets.Clear();
    }
  }
}
