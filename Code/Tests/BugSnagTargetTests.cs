using System;
using System.Diagnostics;
using System.Linq;
using Bugsnag;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Bugsnag;
using NLog.Config;
using Shouldly;
using Xunit;

namespace Tests
{
    public class BugsnagTargetTests
    {
        public static void SetupLogManager(string formattedMessageTab)
        {
            SetupLogManager(LogLevel.Trace, formattedMessageTab);
        }

        public static void SetupLogManager(LogLevel logLevel, string formattedMessageTab = null)
        {
            var loggingConfiguration = new LoggingConfiguration();
            var target = new BugsnagTarget
            {
                ApiKey = "d5dacfd18de492962408fef739c6f36c",
                ReleaseStage = "release",
                Endpoint = TestServer.EndpointUri,
                FormattedMessageTab = formattedMessageTab
            };

            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", logLevel, target));
            loggingConfiguration.AddTarget("BugSnag", target);
            LogManager.Configuration = loggingConfiguration;
        }

        public enum TheoryLoggerEventType
        {
            Error,
            Warning,
            Info,
            Debug,
            Trace
        }

        [Theory]
        [InlineData(TheoryLoggerEventType.Error, null)]
        [InlineData(TheoryLoggerEventType.Warning, null)]
        [InlineData(TheoryLoggerEventType.Info, null)]
        [InlineData(TheoryLoggerEventType.Debug, null)]
        [InlineData(TheoryLoggerEventType.Trace, null)]
        [InlineData(TheoryLoggerEventType.Error, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Warning, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Info, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Debug, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Trace, "Another Tab Name")]
        public void GivenAnExceptionWithNoMetadata_LogMessage__LogsTheException(TheoryLoggerEventType eventType,
            string formattedMessageTab)
        {
            // Arrange.
            SetupLogManager(formattedMessageTab);
            var logger = LogManager.GetLogger("test");
            const string errorMessage = "Something sad happened.";
            var exception = new Exception(errorMessage);

            const string formattedErrorMessage = "pew pew";
            string error;

            // Act.
            using (var testServer = new TestServer())
            {
                LogMessage(eventType, logger)(exception, formattedErrorMessage);
                error = testServer.GetLastResponse();
            }

            // Assert.
            var anotherTabName = string.IsNullOrWhiteSpace(formattedMessageTab)
                ? "Custom Data"
                : formattedMessageTab;
            var result = JObject.Parse(error);
            result["events"][0]["exceptions"][0]["message"].ToString().ShouldBe(errorMessage);
            result["events"][0]["exceptions"][0]["stacktrace"].HasValues.ShouldBe(true);
            result["events"][0]["metaData"].Count().ShouldBe(2);
            result["events"][0]["metaData"][anotherTabName][BugsnagTarget.FormattedMessageKey].ShouldBe(formattedErrorMessage);
            result["events"][0]["severity"].ToString().ShouldBe(MapTheoryLoggerEventTypeToSeverity(eventType));
        }

        [Theory]
        [InlineData(TheoryLoggerEventType.Error, null)]
        [InlineData(TheoryLoggerEventType.Warning, null)]
        [InlineData(TheoryLoggerEventType.Info, null)]
        [InlineData(TheoryLoggerEventType.Debug, null)]
        [InlineData(TheoryLoggerEventType.Trace, null)]
        [InlineData(TheoryLoggerEventType.Error, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Warning, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Info, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Debug, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Trace, "Another Tab Name")]
        public void GivenABugsnagException_LogMessage__LogsTheException(TheoryLoggerEventType eventType,
            string formattedMessageTab)
        {
            // Arrange.
            SetupLogManager(formattedMessageTab);
            var logger = LogManager.GetLogger("test");
            const string errorMessage = "Something sad happened.";
            var exception = new BugsnagException(errorMessage);

            const string formattedErrorMessage = "pew pew";
            string error;

            // Act.
            using (var testServer = new TestServer())
            {
                LogMessage(eventType, logger)(exception, formattedErrorMessage);
                error = testServer.GetLastResponse();
            }

            // Assert.
            var anotherTabName = string.IsNullOrWhiteSpace(formattedMessageTab)
                ? "Custom Data"
                : formattedMessageTab;
            var result = JObject.Parse(error);
            result["events"][0]["exceptions"][0]["errorClass"].ShouldBe("BugsnagException");
            result["events"][0]["exceptions"][0]["message"].ToString().ShouldBe(errorMessage);
            result["events"][0]["exceptions"][0]["stacktrace"].HasValues.ShouldBe(true);
            result["events"][0]["metaData"].Count().ShouldBe(2);
            result["events"][0]["metaData"][anotherTabName][BugsnagTarget.FormattedMessageKey].ShouldBe(formattedErrorMessage);
            result["events"][0]["severity"].ToString().ShouldBe(MapTheoryLoggerEventTypeToSeverity(eventType));
        }

        [Theory]
        [InlineData(TheoryLoggerEventType.Error, null)]
        [InlineData(TheoryLoggerEventType.Warning, null)]
        [InlineData(TheoryLoggerEventType.Info, null)]
        [InlineData(TheoryLoggerEventType.Debug, null)]
        [InlineData(TheoryLoggerEventType.Trace, null)]
        [InlineData(TheoryLoggerEventType.Error, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Warning, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Info, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Debug, "Another Tab Name")]
        [InlineData(TheoryLoggerEventType.Trace, "Another Tab Name")]
        public void GivenABugsnagExceptionAndMetadata_LogMessage_LogsTheException(TheoryLoggerEventType eventType,
            string formattedMessageTab)
        {
            // Arrange.
            SetupLogManager(formattedMessageTab);
            var logger = LogManager.GetLogger("test");
            const string errorMessage = "Something sad happened.";
            const string tabName = "Some Tab name";
            const string metaDataKey = "aaaaa";
            const string metaDataValue = "bbbbb";
            var metadata = new Metadata();
            metadata.AddToTab(tabName, metaDataKey, metaDataValue);
            var exception = new BugsnagException(errorMessage) {Metadata = metadata};

            const string formattedErrorMessage = "pew pew";

            string error;

            // Act.
            using (var testServer = new TestServer())
            {
                LogMessage(eventType, logger)(exception, formattedErrorMessage);
                error = testServer.GetLastResponse();
            }

            // Assert.
            var anotherTabName = string.IsNullOrWhiteSpace(formattedMessageTab)
                ? "Custom Data"
                : formattedMessageTab;
            var result = JObject.Parse(error);
            result["events"][0]["exceptions"][0]["errorClass"].ShouldBe("BugsnagException");
            result["events"][0]["exceptions"][0]["message"].ToString().ShouldBe(errorMessage);
            result["events"][0]["exceptions"][0]["stacktrace"].HasValues.ShouldBe(true);
            result["events"][0]["metaData"].Count().ShouldBe(3);
            result["events"][0]["metaData"][tabName][metaDataKey].ShouldBe(metaDataValue);
            result["events"][0]["metaData"][anotherTabName][BugsnagTarget.FormattedMessageKey].ShouldBe(formattedErrorMessage);
            result["events"][0]["severity"].ToString().ShouldBe(MapTheoryLoggerEventTypeToSeverity(eventType));
        }

        [Theory]
        [InlineData(TheoryLoggerEventType.Error)]
        [InlineData(TheoryLoggerEventType.Warning)]
        [InlineData(TheoryLoggerEventType.Info)]
        [InlineData(TheoryLoggerEventType.Debug)]
        [InlineData(TheoryLoggerEventType.Trace)]
        public void GivenAMessage_LogMessage_LogsTheErrorMessage(TheoryLoggerEventType eventType)
        {
            // Arrange.
            SetupLogManager(null);
            var logger = LogManager.GetLogger("test");
            const string errorMessage = "Something sad happened.";

            string error;

            // Act.
            using (var testServer = new TestServer())
            {
                LogMessageWithNoException(eventType, logger)(errorMessage);
                error = testServer.GetLastResponse();
            }

            // Assert.
            var result = JObject.Parse(error);
            result["events"][0]["exceptions"][0]["errorClass"].ToString().ShouldBe("BugsnagException");
            result["events"][0]["exceptions"][0]["message"].ToString().ShouldBe(errorMessage);
            result["events"][0]["exceptions"][0]["stacktrace"].HasValues.ShouldBe(true);
            result["events"][0]["metaData"].Count().ShouldBe(1);
            result["events"][0]["metaData"]["Exception Details"]["runtimeEnding"].ToString().ShouldBe("False");
        }

        private static string MapTheoryLoggerEventTypeToSeverity(TheoryLoggerEventType eventType)
        {
            string result;

            switch (eventType)
            {
                case TheoryLoggerEventType.Error:
                    result = "error";
                    break;
                case TheoryLoggerEventType.Warning:
                    result = "warning";
                    break;
                default:
                    result = "info";
                    break;
            }

            return result;
        }

        private static Action<Exception, string> LogMessage(TheoryLoggerEventType eventType, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            Action<Exception, string> result;

            switch (eventType)
            {
                case TheoryLoggerEventType.Error: result = (x, y) => logger.Error(x, y);
                    break;
                case TheoryLoggerEventType.Warning: result = (x, y) => logger.Warn(x, y);
                    break;
                case TheoryLoggerEventType.Info: result = (x, y) => logger.Info(x, y);
                    break;
                case TheoryLoggerEventType.Debug: result = (x, y) => logger.Info(x, y);
                    break;
                case TheoryLoggerEventType.Trace: result = (x, y) => logger.Info(x, y);
                    break;
                default:
                    throw new NotImplementedException("damn!");
            }

            return result;
        }

        private static Action<string> LogMessageWithNoException(TheoryLoggerEventType eventType, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            Action<string> result;

            switch (eventType)
            {
                case TheoryLoggerEventType.Error: result = logger.Error;
                    break;
                case TheoryLoggerEventType.Warning: result = logger.Warn;
                    break;
                case TheoryLoggerEventType.Info: result = logger.Info;
                    break;
                case TheoryLoggerEventType.Debug: result = logger.Info;
                    break;
                case TheoryLoggerEventType.Trace: result = logger.Info;
                    break;
                default:
                    throw new NotImplementedException("damn!");
            }

            return result;
        }
    }
}
