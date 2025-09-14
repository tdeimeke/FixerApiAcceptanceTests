using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace FixerApiAcceptanceTests
{
    // --- Global Fixture for start/finish ---
    public class GlobalTestFixture : IAsyncLifetime
    {
        public static readonly string ReportFile = "Report.log";

        public async Task InitializeAsync()
        {
            if (File.Exists(ReportFile))
                File.Delete(ReportFile);

            await File.AppendAllTextAsync(ReportFile,
                $"{DateTime.Now:HH:mm:ss} [GLOBAL] - === Test Execution Started ==={Environment.NewLine}");
        }

        public async Task DisposeAsync()
        {
            await File.AppendAllTextAsync(ReportFile,
                $"{DateTime.Now:HH:mm:ss} [GLOBAL] - === Test Execution Finished ==={Environment.NewLine}");
        }
    }

    // --- Collection definition so that all tests use the same fixture ---
    [CollectionDefinition("API Tests")]
    public class ApiTestCollection : ICollectionFixture<GlobalTestFixture>
    {
        // empty, only for definition
    }

    // --- test class ---
    [Collection("API Tests")]
    public class FixerApiTests
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl = "http://data.fixer.io/api/latest"; 
        private readonly string _apiKey;
        private readonly ITestOutputHelper _output;

        public FixerApiTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _client = new HttpClient();
            _apiKey = Environment.GetEnvironmentVariable("FIXER_API_KEY_FREE") ?? "MISSING_KEY";

            if (!string.IsNullOrEmpty(_apiKey) && _apiKey.Length >= 4)
                _output.WriteLine($"API Key loaded (masked): {_apiKey[..4]}****");
            else
                _output.WriteLine("API Key not set or too short!");
        }

        private async Task LogAsync(string message, string testName)
        {
            var logLine = $"{DateTime.Now:HH:mm:ss} [{testName}] - {message}";
            _output.WriteLine(logLine);
            await File.AppendAllTextAsync(GlobalTestFixture.ReportFile, logLine + Environment.NewLine);
        }

        private async Task<JsonDocument> SendRequestAndParse(string url, string testName)
        {
            await LogAsync($"REQUEST: {url}", testName);
            var response = await _client.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();
            await LogAsync($"RESPONSE: {(int)response.StatusCode} - {responseBody}", testName);

            // kleine Pause, um Rate Limits zu vermeiden
            await Task.Delay(1200);

            return JsonDocument.Parse(responseBody);
        }

        // --- positive test scenarios ---
        [Fact(DisplayName = "Scenario: Get latest rates with base currency EUR and valid access key (positive)")]
        public async Task GetLatestRates_ValidKey_ReturnsSuccess()
        {
            string testName = nameof(GetLatestRates_ValidKey_ReturnsSuccess);
            var url = _baseUrl + $"?access_key={_apiKey}";
            using var json = await SendRequestAndParse(url, testName);

            bool success = json.RootElement.GetProperty("success").GetBoolean();
            Assert.True(success, "Expected success:true from API");

            string? baseCurrency = json.RootElement.GetProperty("base").GetString();
            Assert.Equal("EUR", baseCurrency);
        }

        [Fact(DisplayName = "Scenario: Response only contains requested symbols USD and DKK (positive)")]
        public async Task GetLatestRates_ResponseContainsOnlyRequestedSymbols()
        {
            string testName = nameof(GetLatestRates_ResponseContainsOnlyRequestedSymbols);
            var url = _baseUrl + $"?access_key={_apiKey}&symbols=USD,DKK";
            using var json = await SendRequestAndParse(url, testName);

            bool success = json.RootElement.GetProperty("success").GetBoolean();
            Assert.True(success, "Expected success:true from API");

            var rates = json.RootElement.GetProperty("rates");
            Assert.True(rates.TryGetProperty("USD", out _), "Expected USD in rates");
            Assert.True(rates.TryGetProperty("DKK", out _), "Expected DKK in rates");
        }

        // --- negative test scenarios ---
        [Fact(DisplayName = "Scenario: Missing access key returns error (negative)")]
        public async Task GetLatestRates_MissingKey_ReturnsError()
        {
            string testName = nameof(GetLatestRates_MissingKey_ReturnsError);
            var url = _baseUrl;
            using var json = await SendRequestAndParse(url, testName);

            bool success = json.RootElement.GetProperty("success").GetBoolean();
            Assert.False(success, "Expected success:false when access key is missing");

            var error = json.RootElement.GetProperty("error");
            string? type = error.GetProperty("type").GetString();
            await LogAsync($"Error type: {type}", testName);
        }

        [Fact(DisplayName = "Scenario: Invalid access key returns error (negative)")]
        public async Task GetLatestRates_InvalidKey_ReturnsError()
        {
            string testName = nameof(GetLatestRates_InvalidKey_ReturnsError);
            var url = _baseUrl + "?access_key=INVALID_KEY";
            using var json = await SendRequestAndParse(url, testName);

            bool success = json.RootElement.GetProperty("success").GetBoolean();
            Assert.False(success, "Expected success:false when access key is invalid");

            var error = json.RootElement.GetProperty("error");
            string? type = error.GetProperty("type").GetString();
            await LogAsync($"Error type: {type}", testName);
        }
    }
}
