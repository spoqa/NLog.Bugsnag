using System;

namespace NLog.Bugsnag
{
    public class WarningException : Exception
    {
        public WarningException(string message) : base(message)
        {
        }
    }
}