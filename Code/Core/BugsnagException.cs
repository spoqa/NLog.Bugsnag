using System;
using Bugsnag;

namespace NLog.Bugsnag
{
    public class BugsnagException : Exception, IMetadata
    {
        public BugsnagException(string message) : base(message)
        {
        }

        public BugsnagException(string message, Exception exception) : base(message, exception)
        {
        }

        public Metadata Metadata { get; set; }
    }
}