using System;

namespace NLog.Bugsnag
{
    public class InfoException : Exception
    {
        public InfoException(string message) : base(message)
        {
        }
    }
}