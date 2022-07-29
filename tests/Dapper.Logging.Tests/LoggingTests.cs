using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Dapper.Logging.Configuration;
using Dapper.Logging.Tests.Infra;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Dapper.Logging.Tests
{
    public class LoggingTests
    {
        public LoggingTests(ITestOutputHelper helper)
            => Helper = helper;

        public ITestOutputHelper Helper { get; set; }

        [Fact]
        public void Should_log_opening_of_connection()
        {
            var logger = new TestLogger<IDbConnectionFactory>();
            var innerConnection = Substitute.For<DbConnection>();
            var services = new ServiceCollection()
                .AddSingleton<ILogger<IDbConnectionFactory>>(logger);

            services.AddDbConnectionFactory(
                prv => innerConnection,
                x => x.WithLogLevel(LogLevel.Information),
                ServiceLifetime.Singleton);

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IDbConnectionFactory>();
            
            var connection = factory.CreateConnection();
            connection.Open();
            
            //assert
            innerConnection.Received().Open();
            logger.Messages.Should().HaveCount(1);
            logger.Messages[0].Text.Should().Contain("open");
        }
        
        [Fact]
        public void Should_log_closing_of_connection()
        {
            var logger = new TestLogger<IDbConnectionFactory>();
            var innerConnection = Substitute.For<DbConnection>();
            var services = new ServiceCollection()
                .AddSingleton<ILogger<IDbConnectionFactory>>(logger);

            services.AddDbConnectionFactory(
                prv => innerConnection,
                x => x.WithLogLevel(LogLevel.Information),
                ServiceLifetime.Singleton);

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IDbConnectionFactory>();
            
            var connection = factory.CreateConnection();
            connection.Close();
            
            //assert
            innerConnection.Received().Close();
            logger.Messages.Should().HaveCount(1);
            logger.Messages[0].Text.Should().Contain("close");
        }
        
        [Fact]
        public void Should_log_queries()
        {
            var logger = new TestLogger<IDbConnectionFactory>();
            var innerConnection = Substitute.For<DbConnection>();
            var innerCmd = Substitute.For<DbCommand>();
            var innerParams = Substitute.For<DbParameterCollection>();
            var param = Substitute.For<DbParameter>();
            param.ParameterName.Returns("@id");
            param.Value.Returns("1");
            innerParams.GetEnumerator().Returns(new []{param}.GetEnumerator());
            innerCmd.Parameters.Returns(innerParams);
            innerConnection.CreateCommand().Returns(innerCmd);
            var services = new ServiceCollection()
                .AddSingleton<ILogger<IDbConnectionFactory>>(logger);

            services.AddDbConnectionFactory(
                prv => innerConnection,
                x => x.WithLogLevel(LogLevel.Information)
                    .WithSensitiveDataLogging(),
                ServiceLifetime.Singleton);

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IDbConnectionFactory>();
            
            var connection = factory.CreateConnection();
            var cmd = connection.CreateCommand();
            cmd.ExecuteNonQuery();
            
            //assert
            innerConnection.Received().CreateCommand();
            innerCmd.Received().ExecuteNonQuery();
            
            logger.Messages.Should().HaveCount(1);
            logger.Messages[0].Text.Should().Contain("query");
            logger.Messages[0].State.Should().ContainKey("params");

            if (logger.Messages[0].State["params"] is Dictionary<string, object> paramsDict) {
                //Microsoft.Extensions.Logging v6.0 changed State values from a String to a Dictionary<string, object>
                paramsDict.Should().ContainKey("@id");
                paramsDict["@id"].Should().Be("1");
            } else  {
                //Earlier versions of Microsoft.Extensions.Logging store State values as a string, e.g. in this case: "[@id, 1]"
                logger.Messages[0].State["params"].Should().BeOfType<string>();
                logger.Messages[0].State["params"].ToString().Should().Contain("id");
                logger.Messages[0].State["params"].ToString().Should().Contain("1");
            }
        }
        
        [Fact]
        public void Should_log_queries_with_exceptions()
        {
            var logger = new TestLogger<IDbConnectionFactory>();
            var innerConnection = Substitute.For<DbConnection>();
            var innerCmd = Substitute.For<DbCommand>();
            var innerParams = Substitute.For<DbParameterCollection>();
            var param = Substitute.For<DbParameter>();
            param.ParameterName.Returns("@id");
            param.Value.Returns("1");
            innerParams.GetEnumerator().Returns(new []{param}.GetEnumerator());
            innerCmd.Parameters.Returns(innerParams);
            
            innerCmd.ExecuteNonQuery().ThrowsForAnyArgs<Exception>(); // ⚠️
            
            innerConnection.CreateCommand().Returns(innerCmd);
            var services = new ServiceCollection()
                .AddSingleton<ILogger<IDbConnectionFactory>>(logger);

            services.AddDbConnectionFactory(
                prv => innerConnection,
                x => x.WithLogLevel(LogLevel.Information)
                    .WithSensitiveDataLogging(),
                ServiceLifetime.Singleton);

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IDbConnectionFactory>();
            
            var connection = factory.CreateConnection();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "bla";

            Action a = () => cmd.ExecuteNonQuery();
            a.Should().Throw<Exception>();
            
            //assert
            innerConnection.Received().CreateCommand();
            innerCmd.Received().ExecuteNonQuery();
            
            logger.Messages.Should().HaveCount(1);
            logger.Messages[0].Text.Should().Contain("query");
            logger.Messages[0].State.Should().ContainKey("params");

            if (logger.Messages[0].State["params"] is Dictionary<string, object> paramsDict) {
                //Microsoft.Extensions.Logging v6.0 changed State values from a String to a Dictionary<string, object>
                paramsDict.Should().ContainKey("@id");
                paramsDict["@id"].Should().Be("1");
            } else {
                //Earlier versions of Microsoft.Extensions.Logging store State values as a string, e.g. in this case: "[@id, 1]"
                logger.Messages[0].State["params"].Should().BeOfType<string>();
                logger.Messages[0].State["params"].ToString().Should().Contain("id");
                logger.Messages[0].State["params"].ToString().Should().Contain("1");
            }
        }
    }
}