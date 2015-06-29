using System;

namespace NLog.Bugsnag
{
    public class ErrorException : Exception
    {
        public ErrorException(string message) : base(message)
        {
        }
    }
}