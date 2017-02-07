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
    private bool steamIsOpen;
    private ICompressionHandler compressionHandler;
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

    public async Task<FileInfo> ExtractFile(ContentCatalogueBinaryEntry file)
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

    private async Task<bool> OpenStream()
    {
      if (targetStream != null && steamIsOpen)
        return true;

      if (!await EnsureFileReleased())
        return false;

      targetStream = File.Open(targetFile.FullName, FileMode.Open);
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

    private bool IsTargetLocked()
    {
      FileStream stream = null;

      try
      {
        stream = targetFile.Open(FileMode.Open, FileAccess.Read, FileShare.None);
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
    private void CloseStream()
    {
      targetStream.Close();
      targetStream.Dispose();
      GC.WaitForPendingFinalizers();
      steamIsOpen = false;
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
        if (await OpenStream())
        {
          targetStream.Seek(contentCatalogueOffset, SeekOrigin.Begin);
          targetStream.Write(BitConverter.GetBytes(xmlLength), 0, sizeof(int));
          targetStream.Write(resultBuffer, 0, (int)contentLength);
        }
        else
          throw new UnableToOpenStreamException();

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
      if (await OpenStream())
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
