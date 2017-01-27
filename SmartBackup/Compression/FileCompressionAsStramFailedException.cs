using System;
using System.Runtime.Serialization;

namespace Vibe.Hammer.SmartBackup.Compression
{
  [Serializable]
  internal class FileCompressionAsStramFailedException : Exception
  {
    public FileCompressionAsStramFailedException()
    {
    }

    public FileCompressionAsStramFailedException(string message) : base(message)
    {
    }

    public FileCompressionAsStramFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected FileCompressionAsStramFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}