using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.BugSnag;
using NLog.Config;
using NLog.Layouts;
using Shouldly;
using Xunit;

namespace Tests
{
    public class BugSnagTargetTests
    {
        public static void SetupLogManager()
        {
            SetupLogManager(LogLevel.Trace);
        }

        public static void SetupLogManager(LogLevel logLevel)
        {
            var loggingConfiguration = new LoggingConfiguration();
            var target = new BugSnagTarget
            {
                ApiKey = "d5dacfd18de492962408fef739c6f36c",
                ReleaseStage = "release",
                Endpoint = TestServer.EndpointUri
            };

            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", logLevel, target));
            loggingConfiguration.AddTarget("BugSnag", target);
            LogManager.Configuration = loggingConfiguration;
        }

        [Fact]
        public void GivenAnException_Error_LogsTheException()
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
                logger.Error(exception, "pew pew");
                error = testServer.GetLastResponse();
            }

            // Assert.
            error["events"][0]["exceptions"][0]["message"].ToString().ShouldBe(errorMessage);
            error["events"][0]["exceptions"][0]["stacktrace"].HasValues.ShouldBe(true);
            error["events"][0]["metaData"].Count().ShouldBe(1);
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
    }
}
