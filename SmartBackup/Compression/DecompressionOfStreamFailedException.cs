using System;
using System.Runtime.Serialization;

namespace Vibe.Hammer.SmartBackup.Compression
{
  [Serializable]
  internal class DecompressionOfStreamFailedException : Exception
  {
    private Exception err;

    public DecompressionOfStreamFailedException()
    {
    }

    public DecompressionOfStreamFailedException(string message) : base(message)
    {
    }

    public DecompressionOfStreamFailedException(Exception err)
    {
      this.err = err;
    }

    public DecompressionOfStreamFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected DecompressionOfStreamFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}