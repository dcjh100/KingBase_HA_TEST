using Kdbndp;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace KingBaseTest.Web.Controllers
{
    public class MonitorController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MonitorController> _logger;

        public MonitorController(IConfiguration configuration, ILogger<MonitorController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> CheckHealth(string mode)
        {
            var stopwatch = Stopwatch.StartNew();
            string connectionString = "";

            try
            {
                switch (mode?.ToLower())
                {
                    case "primary":
                        connectionString = _configuration.GetConnectionString("KingBaseHAConnection");
                        break;

                    case "standby":
                        connectionString = _configuration.GetConnectionString("KingBaseHAReadConnection");
                        break;

                    case "loadbalance":
                        // For load balancing, we use the read connection (which has multiple IPs)
                        // and disable pooling to force new connections
                        var baseConn = _configuration.GetConnectionString("KingBaseHAReadConnection");
                        var builder = new KdbndpConnectionStringBuilder(baseConn);
                        builder.Pooling = false;
                        connectionString = builder.ToString();
                        break;

                    default:
                        return Json(new { success = false, error = "Invalid mode. Use 'primary', 'standby', or 'loadbalance'." });
                }

                if (string.IsNullOrEmpty(connectionString))
                {
                    return Json(new { success = false, error = "Connection string not found." });
                }

                using (var connection = new KdbndpConnection(connectionString))
                {
                    await connection.OpenAsync();

                    if (mode?.ToLower() == "primary")
                    {
                        using (var cmd = new KdbndpCommand())
                        {
                            cmd.Connection = connection;

                            // 1. 检查是否有一个叫 HA_WRITE_TEST 的表, 如果没有则创建
                            cmd.CommandText = "CREATE TABLE IF NOT EXISTS HA_WRITE_TEST (test VARCHAR(50));";
                            await cmd.ExecuteNonQueryAsync();

                            // 2. 创建完成后插入一条记录 write
                            cmd.CommandText = "INSERT INTO HA_WRITE_TEST (test) VALUES ('write');";
                            await cmd.ExecuteNonQueryAsync();

                            // 3. 插入完成再删除这条记录
                            cmd.CommandText = "DELETE FROM HA_WRITE_TEST WHERE test = 'write';";
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    string sql = @"
                        SELECT
                            inet_server_addr() AS server_ip,
                            inet_server_port() AS server_port,
                            inet_client_addr() AS client_ip,
                            current_setting('transaction_read_only') AS is_read_only,
                            version() AS version";

                    using (var command = new KdbndpCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            stopwatch.Stop();
                            return Json(new
                            {
                                success = true,
                                timestamp = DateTime.Now,
                                data = new
                                {
                                    serverIp = reader["server_ip"]?.ToString(),
                                    serverPort = reader["server_port"]?.ToString(),
                                    clientIp = reader["client_ip"]?.ToString(),
                                    isReadOnly = reader["is_read_only"]?.ToString(),
                                    version = reader["version"]?.ToString(),
                                    elapsedMs = stopwatch.ElapsedMilliseconds
                                }
                            });
                        }
                    }
                }

                return Json(new { success = false, error = "No data returned from database." });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "CheckHealth failed for mode {Mode}", mode);
                var fullError = GetFullExceptionMessage(ex);
                return Json(new
                {
                    success = false,
                    error = fullError,
                    elapsedMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var messages = new List<string>();
            var current = ex;
            while (current != null)
            {
                messages.Add(current.Message);
                current = current.InnerException;
            }
            return string.Join(" -> ", messages);
        }
    }
}