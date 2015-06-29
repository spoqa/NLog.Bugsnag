using System;
using System.Diagnostics;
using System.Linq;
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
        public static void SetupLogManager()
        {
            SetupLogManager(LogLevel.Trace);
        }

        public static void SetupLogManager(LogLevel logLevel)
        {
            var loggingConfiguration = new LoggingConfiguration();
            var target = new BugsnagTarget
            {
                ApiKey = "d5dacfd18de492962408fef739c6f36c",
                ReleaseStage = "release",
                Endpoint = TestServer.EndpointUri
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
        [InlineData(TheoryLoggerEventType.Error)]
        [InlineData(TheoryLoggerEventType.Warning)]
        [InlineData(TheoryLoggerEventType.Info)]
        [InlineData(TheoryLoggerEventType.Debug)]
        [InlineData(TheoryLoggerEventType.Trace)]
        public void GivenAnException_Error_LogsTheException(TheoryLoggerEventType eventType)
        {
            // Arrange.
            SetupLogManager();
            var logger = LogManager.GetLogger("test");
            const string errorMessage = "Something sad happened.";
            var exception = new Exception(errorMessage);

            JObject error;

            // Act.
            using (var testServer = new TestServer())
            {
                LogMessage(eventType, logger)(exception, "pew pew");
                error = testServer.GetLastResponse();
            }

            // Assert.
            Debug.WriteLine("Debug: " + error.ToString());
            Console.WriteLine("Debug: " + error.ToString());
            Trace.WriteLine("Trace: " + error.ToString());
            error["events"][0]["exceptions"][0]["message"].ToString().ShouldBe(errorMessage);
            error["events"][0]["exceptions"][0]["stacktrace"].HasValues.ShouldBe(true);
            error["events"][0]["metaData"].Count().ShouldBe(1);
            error["events"][0]["severity"].ToString().ShouldBe(MapTheoryLoggerEventTypeToSeverity(eventType));
        }

        [Fact]
        public void GivenAnExceptionWithSomeMetaData_Error_LogsTheException()
        {
            // Arrange.
            SetupLogManager();
            var logger = LogManager.GetLogger("test");
            const string errorMessage = "Something sad happened.";
            var exception = new Exception(errorMessage);

            var metaData = new[]
            {
                new Tuple<string, string>("a1", "b1"),
                new Tuple<string, string>("a2", "b2")
            };

            JObject error;

            // Act.
            using (var testServer = new TestServer())
            {
                logger.Error(exception, "pew pew", metaData);
                error = testServer.GetLastResponse();
            }

            // Assert.
            var ss = error.ToString(Formatting.None);
            error["events"][0]["exceptions"][0]["message"].ToString().ShouldBe(errorMessage);
            error["events"][0]["exceptions"][0]["stacktrace"].HasValues.ShouldBe(true);
            error["events"][0]["metaData"].Count().ShouldBe(2);
            error["events"][0]["metaData"]["Extra Information"]["a1"].ToString().ShouldBe("b1");
            error["events"][0]["metaData"]["Extra Information"].Count().ShouldBe(2);
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
    }
}
