using System;
using System.Collections;
using System.Linq;
using Bugsnag;
using Bugsnag.Clients;
using NLog.Config;
using NLog.Targets;

namespace NLog.Bugsnag
{
    [Target("Bugsnag")]
    public class BugsnagTarget : Target
    {
        private const string MetaDataTabName = "Extra Information";

        private readonly Lazy<BaseClient> _baseClient;
 
        public BugsnagTarget()
        {
            _baseClient = new Lazy<BaseClient>(() =>
            {
                var Bugsnag = new BaseClient(ApiKey);
                Bugsnag.Config.ReleaseStage = ReleaseStage;
                if (!string.IsNullOrWhiteSpace(Endpoint))
                {
                    Bugsnag.Config.Endpoint = Endpoint;
                }
                return Bugsnag;
            });
        }

        [RequiredParameter]
        public string ApiKey { get; set; }

        [RequiredParameter]
        public string ReleaseStage { get; set; }

        public string MetaDataTab { get; set; }

        public string Endpoint { get; set; }

        protected override void Write(LogEventInfo logEvent)
        {
            Metadata metaData = null;

            if (logEvent.Parameters != null)
            {
                metaData = ToMetadata(logEvent.Parameters);
            }

            if (logEvent.Exception != null)
            {
                _baseClient.Value.Notify(logEvent.Exception, logEvent.Level.ToSeverity(), metaData);
            }
            else if (string.IsNullOrWhiteSpace(logEvent.Message))
            {
                var exception = new Exception(logEvent.Message);
                _baseClient.Value.Notify(exception, logEvent.Level.ToSeverity(), metaData);
            }
        }

        private Metadata ToMetadata(IEnumerable parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            // Lets see if we have some key/values.
            var doubleTuple = parameters as Tuple<string, string>[];
            if (doubleTuple != null && 
                doubleTuple.Any())
            {
                return ToMetadata(doubleTuple);
            }

            var trippleTuple = parameters as Tuple<string, string, string>[];
            if (trippleTuple != null &&
                trippleTuple.Any())
            {
                return ToMetadata(trippleTuple);
            }

            return null;
        }

        private Metadata ToMetadata(Tuple<string, string>[] keyValues)
        {
            if (keyValues == null)
            {
                throw new ArgumentNullException();
            }

            Metadata metaData = null;

            foreach (var keyValue in keyValues)
            {
                if (metaData == null)
                {
                    metaData = new Metadata();
                }

                metaData.AddToTab(string.IsNullOrWhiteSpace(MetaDataTab)
                    ? MetaDataTabName
                    : MetaDataTab,
                    keyValue.Item1,
                    keyValue.Item2);
            }

            return metaData;
        }

        private static Metadata ToMetadata(Tuple<string, string, string>[] keyValues)
        {
            if (keyValues == null)
            {
                throw new ArgumentNullException();
            }

            Metadata metaData = null;

            foreach (var keyValue in keyValues)
            {
                if (metaData == null)
                {
                    metaData = new Metadata();
                }

                metaData.AddToTab(keyValue.Item1,
                    keyValue.Item2,
                    keyValue.Item3);
            }

            return metaData;
        }
    }
}