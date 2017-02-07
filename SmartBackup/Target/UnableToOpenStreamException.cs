using System;
using System.Runtime.Serialization;

namespace Vibe.Hammer.SmartBackup
{
  [Serializable]
  internal class UnableToOpenStreamException : Exception
  {
    public UnableToOpenStreamException()
    {
    }

    public UnableToOpenStreamException(string message) : base(message)
    {
    }

    public UnableToOpenStreamException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected UnableToOpenStreamException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}