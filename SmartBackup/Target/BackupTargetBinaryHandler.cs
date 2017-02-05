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
  public class BackupTargetBinaryHandler : IBackupTargetBinaryHandler, IDisposable
  {
    private FileInfo targetFile;
    private int megaByte = Convert.ToInt32(Math.Pow(2, 20));
    private int contentCatalogueOffset = BackupTargetConstants.ContentCatalogueOffset;
    private FileStream targetStream;
    private bool openForWriting;
    private bool openForReading;
    private ICompressionHandler compressionHandler;
    private bool fileCreated = false;
    private List<string> tempFilenames = new List<string>();

    public bool BinaryFileExists => targetFile.Exists;

    public BackupTargetBinaryHandler(FileInfo targetFile, ICompressionHandler compressionHandler)
    {
      this.targetFile = targetFile;
      this.compressionHandler = compressionHandler;
      if (!targetFile.Exists)
      {
        File.Copy(@".\SmartBackup.exe", targetFile.FullName);
        GC.WaitForPendingFinalizers();
      }
      fileCreated = true;
    }

    ~BackupTargetBinaryHandler()
    {
      Dispose(false);
    }

    public Task<bool> Defragment()
    {
      throw new NotImplementedException();
    }

    public async Task<bool> InsertFile(ContentCatalogueBinaryEntry file, FileInfo sourceFile)
    {
      if (!sourceFile.Exists)
        throw new FileNotFoundException(sourceFile.FullName);

      try
      {
        if (OpenStreamForWriting())
        {
          targetStream.Seek(file.TargetOffset, SeekOrigin.Begin);
          using (var sourceStream = File.OpenRead(sourceFile.FullName))
          {
            await sourceStream.CopyToAsync(targetStream);
          }
          return true;
        }
      }
      catch (Exception err)
      {
        Console.WriteLine(err.ToString());
      }
      return false;
    }

    public async Task<FileInfo> ExtractFile(ContentCatalogueBinaryEntry file)
    {
      DeleteTempFiles();
      var tempFile = GetTempFile();
      try
      {
        if (OpenStreamForReading())
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

    private bool OpenStreamForWriting()
    {
      if (targetStream != null && openForWriting)
        return true;

      if (targetStream != null && openForReading)
      {
        targetStream.Close();
        targetStream.Dispose();
        GC.WaitForPendingFinalizers();
      }

      targetStream = File.OpenWrite(targetFile.FullName);
      openForWriting = true;
      openForReading = false;
      return true;
    }


    private bool OpenStreamForReading()
    {
      if (targetStream != null && openForReading)
        return true;

      if (targetStream != null && openForWriting)
      {
        targetStream.Close();
        targetStream.Dispose();
        GC.WaitForPendingFinalizers();
      }

      targetStream = File.OpenRead(targetFile.FullName);
      openForWriting = false;
      openForReading = true;
      return true;
    }

    private bool OpenStreamForReadWrite()
    {
      if (targetStream != null && openForReading && openForWriting)
        return true;

      if (targetStream != null && openForWriting)
      {
        targetStream.Close();
        targetStream.Dispose();
        GC.WaitForPendingFinalizers();
      }

      if (targetStream != null && openForReading)
      {
        targetStream.Close();
        targetStream.Dispose();
        GC.WaitForPendingFinalizers();
      }

      targetStream = File.Open(targetFile.FullName, FileMode.Open);
      openForWriting = true;
      openForReading = true;
      return true;
    }

    private void CloseStream()
    {
      targetStream.Close();
      targetStream.Dispose();
      GC.WaitForPendingFinalizers();
      openForWriting = false;
      openForReading = false;
    }

    public Task<bool> RemoveFile(ContentCatalogueBinaryEntry file)
    {
      throw new NotImplementedException();
    }

    public async Task WriteContentCatalogue(ContentCatalogue catalogue, bool closeStreams)
    {
      try
      {
        XmlSerializer xsSubmit = new XmlSerializer(typeof(ContentCatalogue));
        byte[] resultBuffer;
        int contentLength;
        int xmlLength = 0;
        using (var resultStream = new MemoryStream())
        {
          using (var xmlStream = new MemoryStream())
          {
            xsSubmit.Serialize(xmlStream, catalogue);
            xmlStream.Position = 0;
            xmlLength = (int)xmlStream.Length;
            xmlStream.Position = 0;
            if (!await compressionHandler.CompressStream(xmlStream, resultStream))
              throw new Exception("Failed to compress");
          }
          resultBuffer = resultStream.ToArray();
        }
        contentLength = resultBuffer.Length;
        if (OpenStreamForWriting())
        {
          targetStream.Seek(contentCatalogueOffset, SeekOrigin.Begin);
          targetStream.Write(BitConverter.GetBytes(xmlLength), 0, sizeof(int));
          targetStream.Write(resultBuffer, 0, (int)contentLength);
        }

        if (closeStreams)
          CloseStream();
      }
      catch (Exception err)
      {

        throw;
      }
    }

    public async Task<ContentCatalogue> ReadContentCatalogue()
    {
      if (OpenStreamForReading())
      {
        try
        {
          var xmlLength = ReadContentLength();
          byte[] decompressedBytes = new byte[xmlLength];
          int read = 0;
          int offset = 0;
          using (DeflateStream decompressionStream = new DeflateStream(targetStream, CompressionMode.Decompress))
          {
            do
            {
              read = await decompressionStream.ReadAsync(decompressedBytes, offset, xmlLength - offset);
              offset += read;
            } while (offset < xmlLength);
          }
          CloseStream();
          return await CreateContentCatalogueFromBytes(decompressedBytes, xmlLength);
        }
        catch (Exception err)
        {
          return null;
        }
      }
      return null;
    }

    private int ReadContentLength()
    {
      targetStream.Seek(contentCatalogueOffset, SeekOrigin.Begin);
      byte[] lengthBuffer = new byte[4];
      targetStream.Read(lengthBuffer, 0, sizeof(int));
      return BitConverter.ToInt32(lengthBuffer, 0);
    }
    private async Task<ContentCatalogue> CreateContentCatalogueFromBytes(byte[] binaryContentCatalogue, int length)
    {
      using (MemoryStream decompressedStream = new MemoryStream())
      {
        await decompressedStream.WriteAsync(binaryContentCatalogue, 0, length);
        decompressedStream.Position = 0;
        var xmlSerializer = new XmlSerializer(typeof(ContentCatalogue));
        var catalogue = xmlSerializer.Deserialize(decompressedStream) as ContentCatalogue;
        catalogue.RebuildSearchIndex();
        return catalogue;
      }
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
          openForReading = false;
          openForWriting = false;
          DeleteTempFiles();
        }
      }
    }

    public async Task MoveBytes(long moveFromOffset, long numberOfBytesToMove, long newOffset)
    {
      if (OpenStreamForReadWrite())
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
    }

    public async Task RetainDataIntervals(OffsetAndLengthPair[] intervals, int lowTail, IProgress<ProgressReport> progressCallback)
    {
      DeleteTempFiles();
      progressCallback.Report(new ProgressReport("Writing temporary file..."));
      var currentFile = 0;
      var numberOfFiles = intervals.Length;
      var tempFile = GetTempFile();
      var time = DateTime.Now;

      if (OpenStreamForReading())
      {
        using (var tempStream = tempFile.Create())
        {
          foreach (var interval in intervals)
          {
            await CopyBytesToStream(targetStream, tempStream, interval);
            currentFile++;
            if (DateTime.Now - time > TimeSpan.FromSeconds(5))
            {
              progressCallback.Report(new ProgressReport($"File {currentFile} of {numberOfFiles}", currentFile, numberOfFiles));
              time = DateTime.Now;
            }
          }
        }
      }

      currentFile = 0;

      if (OpenStreamForWriting())
      {
        var initialLength = targetStream.Length;
        using (var tempStream = tempFile.OpenRead())
        {
          targetStream.Position = lowTail;
          progressCallback.Report(new ProgressReport("Copying defragmented file back into catalogue..."));
          await CopyBytesToStream(tempStream, targetStream);
        }
        tempFile.Delete();
      }
    }

    private async Task CopyBytesToStream(FileStream source, FileStream target, OffsetAndLengthPair interval)
    {
      source.Position = interval.Offset;
      var bytesRead = 0;
      var buffer = new byte[1000000];
      do
      {
        var read = source.Read(buffer, 0, (int)Math.Min(interval.Length, 1000000));
        if (read > 0)
        {
          await target.WriteAsync(buffer, 0, read);
          bytesRead += read;
        }
      } while (bytesRead < interval.Length);
    }

    private async Task CopyBytesToStream(FileStream source, FileStream target)
    {
      source.Seek(0L, SeekOrigin.Begin);
      await source.CopyToAsync(target);
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
