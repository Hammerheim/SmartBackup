using System;
using System.Runtime.Serialization;

namespace Vibe.Hammer.SmartBackup
{
  [Serializable]
  internal class BackupTargetNotInitializedException : Exception
  {
    public BackupTargetNotInitializedException()
    {
    }

    public BackupTargetNotInitializedException(string message) : base(message)
    {
    }

    public BackupTargetNotInitializedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected BackupTargetNotInitializedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}