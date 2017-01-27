using System;
using System.Runtime.Serialization;

namespace Vibe.Hammer.SmartBackup.Compression
{
  [Serializable]
  internal class FileCompressionAsArchiveFailedException : Exception
  {
    public FileCompressionAsArchiveFailedException()
    {
    }

    public FileCompressionAsArchiveFailedException(string message) : base(message)
    {
    }

    public FileCompressionAsArchiveFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected FileCompressionAsArchiveFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}