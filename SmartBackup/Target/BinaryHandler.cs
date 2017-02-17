using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Compression;
using Vibe.Hammer.SmartBackup.Progress;
using Vibe.Hammer.SmartBackup.Target;

namespace Vibe.Hammer.SmartBackup
{
  public class BinaryHandler : IBinaryHandler, IDisposable
  {
    private int megaByte = Convert.ToInt32(Math.Pow(2, 20));
    private bool steamIsOpen;
    private List<string> tempFilenames = new List<string>();

    protected ICompressionHandler CompressionHandler { get; private set; }
    protected FileStream targetStream { get; private set; }

    protected FileInfo TargetFile { get; private set; }

    public bool BinaryFileExists => TargetFile.Exists;

    public BinaryHandler(FileInfo targetFile, ICompressionHandler compressionHandler)
    {
      this.TargetFile = targetFile;
      this.CompressionHandler = compressionHandler;
    }

    ~BinaryHandler()
    {
      Dispose(false);
    }

    public virtual Task<bool> Defragment()
    {
      throw new NotImplementedException();
    }

    public virtual async Task<bool> InsertFile(ContentCatalogueBinaryEntry file, FileInfo sourceFile)
    {
      if (!sourceFile.Exists)
        throw new FileNotFoundException(sourceFile.FullName);

      try
      {
        if (await OpenStream())
        {
          targetStream.Seek(file.TargetOffset, SeekOrigin.Begin);
          using (var sourceStream = File.OpenRead(sourceFile.FullName))
          {
            await sourceStream.CopyToAsync(targetStream);
          }
          return true;
        }
        else
          throw new UnableToOpenStreamException();
      }
      catch (Exception err)
      {
        Console.WriteLine(err.ToString());
      }
      return false;
    }

    public virtual async Task<FileInfo> ExtractFile(ContentCatalogueBinaryEntry file)
    {
      DeleteTempFiles();
      var tempFile = GetTempFile();
      try
      {
        if (await OpenStream())
        {
          if (targetStream.Length < file.TargetOffset)
            throw new IndexOutOfRangeException();

          targetStream.Seek(file.TargetOffset, SeekOrigin.Begin);
          await CopyBytesToFile(file, tempFile.FullName);
          return tempFile;
        }
        return null;
      }
      catch (Exception err)
      {
        throw;
      }
    }

    private async Task CopyBytesToFile(ContentCatalogueBinaryEntry file, string tempFilename)
    {
      using (var fileStream = new FileStream(tempFilename, FileMode.Create))
      {
        var buffer = new byte[1000000];
        var missing = file.TargetLength;
        do
        {
          var expectToRead = (int)Math.Min(missing, 1000000);
          var read = targetStream.Read(buffer, 0, expectToRead);
          if (read > 0)
          {
            await fileStream.WriteAsync(buffer, 0, read);
            missing -= read;
          }
        } while (missing > 0);
      }
      GC.WaitForPendingFinalizers();
    }

    protected async Task<bool> OpenStream()
    {
      if (targetStream != null && steamIsOpen)
        return true;

      if (!await EnsureFileReleased())
        return false;

      targetStream = File.Open(TargetFile.FullName, FileMode.Open);
      steamIsOpen = true;
      return true;
    }

    private async Task<bool> EnsureFileReleased()
    {
      int tries = 0;
      while (IsTargetLocked() && ++tries < 10)
      {
        await Task.Delay(1000);
      }
      await Task.Delay(1000);
      return tries < 10;
    }

    protected bool IsTargetLocked()
    {
      FileStream stream = null;

      if (!TargetFile.Exists)
      {
        TargetFile.Create().Close();
        GC.WaitForPendingFinalizers();
      }

      try
      {
        stream = TargetFile.Open(FileMode.Open, FileAccess.Read, FileShare.None);
      }
      catch (IOException)
      {
        return true;
      }
      finally
      {
        if (stream != null)
          stream.Close();
      }

      //file is not locked
      return false;
    }
    public void CloseStream()
    {
      if (targetStream != null)
      {
        targetStream.Close();
        targetStream.Dispose();
        GC.WaitForPendingFinalizers();
      }
      steamIsOpen = false;
    }

    public virtual Task<bool> RemoveFile(ContentCatalogueBinaryEntry file)
    {
      throw new NotImplementedException();
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected void Dispose(bool managedDispose)
    {
      if (managedDispose)
      {
        if (targetStream != null)
        {
          targetStream.Close();
          targetStream.Dispose();
          targetStream = null;
          steamIsOpen = false;
          DeleteTempFiles();
        }
      }
    }

    public async Task MoveBytes(long moveFromOffset, long numberOfBytesToMove, long newOffset)
    {
      if (await OpenStream())
      {
        long remainingBytes = numberOfBytesToMove;
        long currentSourceOffset = moveFromOffset;
        long currentTargetOffset = newOffset;
        byte[] buffer = new byte[BackupTargetConstants.BufferSize];
        do
        {
          targetStream.Seek(currentSourceOffset, SeekOrigin.Begin);
          var read = await targetStream.ReadAsync(buffer, 0, (int)Math.Min(remainingBytes, BackupTargetConstants.BufferSize));
          currentSourceOffset += read;
          if (read > 0)
          {
            targetStream.Seek(currentTargetOffset, SeekOrigin.Begin);
            await targetStream.WriteAsync(buffer, 0, read);
            currentTargetOffset += read;
          }
          remainingBytes -= read;
        } while (remainingBytes > 0);
      }
      else
        throw new UnableToOpenStreamException();
    }

    private FileInfo GetTempFile()
    {
      var path = Path.GetTempPath();
      var file = Path.Combine(path, Guid.NewGuid().ToString());
      file += ".tmp";
      tempFilenames.Add(file);
      return new FileInfo(file);
    }

    private void DeleteTempFiles()
    {
      GC.WaitForPendingFinalizers();
      var files = tempFilenames.ToArray();
      foreach (var file in files)
      {
        try
        {
          File.Delete(file);
          tempFilenames.Remove(file);
        }
        catch
        {
          continue;
        }
      }
    }
  }
}
