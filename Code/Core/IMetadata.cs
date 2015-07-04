using Bugsnag;

namespace NLog.Bugsnag
{
    public interface IMetadata
    {
        Metadata Metadata { get; set; }
    }
}