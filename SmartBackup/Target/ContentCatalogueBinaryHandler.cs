using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Compression;

namespace Vibe.Hammer.SmartBackup.Target
{
  public class ContentCatalogueBinaryHandler : BinaryHandler
  {

    public ContentCatalogueBinaryHandler(FileInfo targetFile, ICompressionHandler compressionHandler)
      : base(targetFile, compressionHandler)
    {

    }

    public virtual void WriteContentCatalogue(ContentCatalogue catalogue, bool closeStreams)
    {
      //try
      //{
        var serializer = new XmlSerializer(typeof(ContentCatalogue));
        using (var writer = new StreamWriter(TargetFile.FullName))
        {
          serializer.Serialize(writer, catalogue);
          writer.Close();
        }

      //      byte[] resultBuffer;
      //  int contentLength;
      //  int xmlLength = 0;
      //  using (var resultStream = new MemoryStream())
      //  {
      //    using (var xmlStream = new MemoryStream())
      //    {
      //      xsSubmit.Serialize(xmlStream, catalogue);
      //      xmlStream.Position = 0;
      //      xmlLength = (int)xmlStream.Length;
      //      xmlStream.Position = 0;
      //      if (!await CompressionHandler.CompressStream(xmlStream, resultStream))
      //        throw new Exception("Failed to compress");
      //    }
      //    resultBuffer = resultStream.ToArray();
      //  }
      //  contentLength = resultBuffer.Length;
      //  if (await OpenStream())
      //  {
      //    targetStream.Seek(0, SeekOrigin.Begin);
      //    targetStream.Write(BitConverter.GetBytes(xmlLength), 0, sizeof(int));
      //    targetStream.Write(resultBuffer, 0, (int)contentLength);
      //  }
      //  else
      //    throw new UnableToOpenStreamException();

      //  if (closeStreams)
      //    CloseStream();
      //}
      //catch (Exception err)
      //{

      //  throw;
      //}
    }

    public virtual ContentCatalogue ReadContentCatalogue()
    {
      if (!TargetFile.Exists)
        return null;

      XmlSerializer serializer = new XmlSerializer(typeof(ContentCatalogue));
      using (var fs = new FileStream(TargetFile.FullName, FileMode.Open))
      {
        return serializer.Deserialize(fs) as ContentCatalogue;
      }

      //  if (await OpenStream())
      //  {
      //    try
      //    {
      //      var xmlLength = ReadContentLength();
      //      byte[] decompressedBytes = new byte[xmlLength];
      //      int read = 0;
      //      int offset = 0;
      //      using (DeflateStream decompressionStream = new DeflateStream(targetStream, CompressionMode.Decompress))
      //      {
      //        do
      //        {
      //          read = await decompressionStream.ReadAsync(decompressedBytes, offset, xmlLength - offset);
      //          offset += read;
      //        } while (offset < xmlLength);
      //      }
      //      CloseStream();
      //      return await CreateContentCatalogueFromBytes(decompressedBytes, xmlLength);
      //    }
      //    catch (Exception err)
      //    {
      //      return null;
      //    }
      //  }
      //return null;
    }

    //private async Task EnsureContentCatalogue(ContentCatalogue catalogue)
    //{
    //  this.catalogue = catalogue;
    //  if (binaryHandler.BinaryFileExists)
    //  {
    //    var tempCatalogue = await binaryHandler.ReadContentCatalogue();
    //    if (tempCatalogue != null)
    //    {
    //      maxLength = tempCatalogue.MaxSizeOfFiles * BackupTargetConstants.MegaByte;
    //      if (this.catalogue != null)
    //        catalogue.EnsureTargetCatalogueExists(tempCatalogue, ID, this);
    //      else
    //        catalogue = tempCatalogue;
    //    }
    //    else
    //      throw new InvalidBinaryTargetException(ID);
    //  }
    //  else
    //  {
    //    catalogue = new ContentCatalogue();
    //    catalogue.Add(new TargetContentCatalogue(ID, this));
    //  }
    //}

    private int ReadContentLength()
    {
      targetStream.Seek(0, SeekOrigin.Begin);
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


  }
}
