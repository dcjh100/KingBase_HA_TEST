using System.Data;
using Microsoft.Extensions.Configuration;
using Kdbndp;
using Xunit.Abstractions;

namespace KingBaseTest.Tests
{
    public class DatabaseConnectionTests
    {
        private readonly IConfiguration _configuration;

        public DatabaseConnectionTests(ITestOutputHelper output)
        {
            // Build configuration manually to read appsettings.json
            // We point to the Web project's appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../src/KingBaseTest.Web"))
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
            Output = output;
        }

        public ITestOutputHelper Output { get; }

        [Fact]
        public void CanConnectToKingBaseAndSelectOne()
        {
            // Arrange
            var connectionString = _configuration.GetConnectionString("KingBaseHAConnection");
            Assert.NotNull(connectionString);
            Assert.NotEmpty(connectionString);
            
            Output.WriteLine($"Connection String (Primary): {connectionString}");

            // Act & Assert
            // Primary should be ReadWrite (transaction_read_only = off)
            VerifyConnection(connectionString, "off");
        }

        [Fact]
        public void CanConnectToKingBaseReadOnlyAndSelectOne()
        {
            // Arrange
            var connectionString = _configuration.GetConnectionString("KingBaseHAReadConnection");
            Assert.NotNull(connectionString);
            Assert.NotEmpty(connectionString);

            Output.WriteLine($"Connection String (ReadOnly): {connectionString}");

            // Act & Assert
            // Standby should be ReadOnly (transaction_read_only = on)
            VerifyConnection(connectionString, "on");
        }

        [Fact]
        public void VerifyLoadBalancingBehavior()
        {
            // Arrange
            var baseConnectionString = _configuration.GetConnectionString("KingBaseHAReadConnection");
            
            // To verify load balancing, we MUST disable connection pooling.
            // Otherwise, Kdbndp will reuse the first successful connection from the pool,
            // and we will see the same server port every time.
            var builder = new KdbndpConnectionStringBuilder(baseConnectionString);
            builder.Pooling = false;
            var connectionString = builder.ToString();

            Output.WriteLine("Testing Load Balancing (Pooling=false)...");
            
            var connectedIps = new List<string>();
            int attempts = 10;

            // Act
            for (int i = 0; i < attempts; i++)
            {
                using (var connection = new KdbndpConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        // User Note: The internal ports (backend) are likely the same (e.g. 54321).
                        // We must check the Server IP (inet_server_addr) to distinguish nodes.
                        using (var command = new KdbndpCommand("SELECT inet_server_addr()", connection))
                        {
                            var ip = command.ExecuteScalar()?.ToString();
                            if (ip != null)
                            {
                                connectedIps.Add(ip);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Output.WriteLine($"Attempt {i + 1} failed: {ex.Message}");
                    }
                }
            }

            // Assert & Report
            var grouped = connectedIps.GroupBy(p => p)
                                        .Select(g => new { Ip = g.Key, Count = g.Count() })
                                        .ToList();

            Output.WriteLine($"Total Attempts: {attempts}");
            Output.WriteLine("Distribution (Server IPs):");
            foreach (var g in grouped)
            {
                Output.WriteLine($"  IP {g.Ip}: {g.Count} times");
            }

            // Note: It's theoretically possible (though unlikely) to hit the same server 10 times in a row with random selection.
            // But if we see > 1 unique IPs, we know LB is working.
            // User requirement: Assert failure if only one IP is found.
            if (grouped.Count > 1)
            {
                Output.WriteLine("SUCCESS: Connected to multiple different IPs.");
            }
            else
            {
                Output.WriteLine("FAILURE: Connected to only one IP. Check if other nodes are down or if LB is misconfigured.");
            }
            
            Assert.True(grouped.Count > 1, "Load balancing check failed: Connected to only one IP address across multiple attempts.");
            Assert.NotEmpty(connectedIps);
        }

        private void VerifyConnection(string connectionString, string expectedReadOnlyState)
        {
            using (var connection = new KdbndpConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    
                    // Verify state is open
                    Assert.Equal(ConnectionState.Open, connection.State);

                    // 1. Execute "select 1 from dual" (Basic connectivity)
                    using (var command = new KdbndpCommand("select 1 from dual", connection))
                    {
                        var result = command.ExecuteScalar();
                        Assert.NotNull(result);
                        Assert.Equal("1", result.ToString());
                    }

                    // 2. Verify ReadOnly status
                    using (var command = new KdbndpCommand("SELECT current_setting('transaction_read_only')", connection))
                    {
                        var result = command.ExecuteScalar();
                        Output.WriteLine($"Transaction ReadOnly Status: {result}");
                        Assert.NotNull(result);
                        Assert.Equal(expectedReadOnlyState, result.ToString());
                    }

                    // 3. Print Network Info
                    PrintNetworkInfo(connection);
                }
                catch (Exception ex)
                {
                    Output.WriteLine($"Connection result: {ex.Message}");
                    
                    if (ex is FormatException || ex is ArgumentException)
                    {
                         throw;
                    }
                    // Re-throw to show test failure in report
                    throw;
                }
            }
        }

        private void PrintNetworkInfo(KdbndpConnection connection)
        {
            string sql = @"
                SELECT 
                    inet_server_addr() AS server_ip,
                    inet_server_port() AS server_port,
                    inet_client_addr() AS client_ip,
                    inet_client_port() AS client_port,
                    current_user AS current_user,
                    session_user AS session_user,
                    current_schema() AS current_schema,
                    version() AS version";

            using (var command = new KdbndpCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    Output.WriteLine("---------- Network Info ----------");
                    Output.WriteLine($"Server IP:   {reader["server_ip"]}");
                    Output.WriteLine($"Server Port: {reader["server_port"]}");
                    Output.WriteLine($"Client IP:   {reader["client_ip"]}");
                    Output.WriteLine($"Client Port: {reader["client_port"]}");
                    Output.WriteLine($"Current User:{reader["current_user"]}");
                    Output.WriteLine($"Session User:{reader["session_user"]}");
                    Output.WriteLine($"Schema:      {reader["current_schema"]}");
                    Output.WriteLine($"Version:     {reader["version"]}");
                    Output.WriteLine("----------------------------------");
                }
            }
        }
    }
}
