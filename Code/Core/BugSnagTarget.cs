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
        public static string FormattedMessageKey = "Message";
        private readonly Lazy<BaseClient> _baseClient;

        public BugsnagTarget()
        {
            _baseClient = new Lazy<BaseClient>(() =>
            {
                var bugsnag = new BaseClient(ApiKey);
                bugsnag.Config.ReleaseStage = ReleaseStage;
                if (!string.IsNullOrWhiteSpace(Endpoint))
                {
                    bugsnag.Config.Endpoint = Endpoint;
                }
                return bugsnag;
            });
        }

        [RequiredParameter]
        public string ApiKey { get; set; }

        [RequiredParameter]
        public string ReleaseStage { get; set; }

        public string FormattedMessageTab { get; set; }

        public string Endpoint { get; set; }

        protected override void Write(LogEventInfo logEvent)
        {
            // A log event can be either an exception OR a message.
            // If both were provided, then the exception takes precedence over the message.
            if (logEvent.Exception != null)
            {
                Metadata metaData = null;

                // Do we have any metadata
                var bugsnagInterface = logEvent.Exception as IMetadata;
                if (bugsnagInterface != null)
                {
                    metaData = bugsnagInterface.Metadata;
                }

                AddFormattedMessageToMetadata(ref metaData, logEvent.FormattedMessage);

                // Notify Bugsnag of this exception.
                _baseClient.Value.Notify(logEvent.Exception, logEvent.Level.ToSeverity(), metaData);
            }
            else if (!string.IsNullOrWhiteSpace(logEvent.Message))
            {
                // We don't have an exception but we do have a message!
                var exception = new BugsnagException(logEvent.Message);
                _baseClient.Value.Notify(exception, logEvent.Level.ToSeverity());
            }
        }

        private void AddFormattedMessageToMetadata(ref Metadata metadata, string formattedMessage)
        {
            if (string.IsNullOrWhiteSpace(formattedMessage))
            {
                return;
            }

            if (metadata == null)
            {
                metadata = new Metadata();
            }

            if (!string.IsNullOrWhiteSpace(FormattedMessageTab))
            {
                metadata.AddToTab(FormattedMessageTab, FormattedMessageKey, formattedMessage);
            }
            else
            {
                metadata.AddToTab(FormattedMessageKey, formattedMessage);
            }
        }
    }
}