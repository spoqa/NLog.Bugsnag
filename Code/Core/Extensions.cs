using Bugsnag;

namespace NLog.BugSnag
{
    public static class Extensions
    {
        public static Severity ToSeverity(this LogLevel logLevel)
        {
            return logLevel >= LogLevel.Error
                ? Severity.Error
                : logLevel >= LogLevel.Warn
                    ? Severity.Warning
                    : Severity.Info;
        }
    }
}