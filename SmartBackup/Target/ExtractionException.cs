using System;
using System.Runtime.Serialization;

namespace Vibe.Hammer.SmartBackup
{
  [Serializable]
  internal class ExtractionException : Exception
  {
    public ExtractionException()
    {
    }

    public ExtractionException(string message) : base(message)
    {
    }

    public ExtractionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected ExtractionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}