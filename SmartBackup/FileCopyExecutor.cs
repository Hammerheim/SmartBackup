using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class FileCopyExecutor : IDisposable
  {
    private string targetFilename;
    private bool deleted;

    ~FileCopyExecutor()
    {
      Dispose(false);
    }

    public async Task<FileInfo> CopyFile(string source)
    {
      try
      {
        targetFilename = Path.GetTempFileName();
        var sourceFile = new FileInfo(source);
        var result = await Task.Run(() => sourceFile.CopyTo(targetFilename, true));

        return new FileInfo(targetFilename);

      }
      catch (Exception)
      {
        return null;
      }
    }

    public void Delete()
    {
      try
      {
        if (!deleted)
        {
          var targetFile = new FileInfo(targetFilename);
          targetFile.Delete();
          deleted = true;
        }
      }
      catch (SecurityException)
      {

      }
      catch (UnauthorizedAccessException)
      {

      }
      catch (IOException)
      {

      }
    }
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool managedDispose)
    {
      if (managedDispose)
      {
        Delete();
      }
    }
  }
}
