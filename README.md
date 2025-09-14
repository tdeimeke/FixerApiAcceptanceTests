# Fixer.io API Acceptance Tests

This project contains a minimal set of automated acceptance tests for the [Fixer.io currency API](https://fixer.io/).  
It is implemented in **.NET 8 / C#** using **xUnit** and runs easily with Visual Studio Code or via the command line.  

The goal is to test one endpoint (`/latest`) with both **positive and negative scenarios**, and to produce a simple log file with requests and responses.

---

## 🔧 Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/tdeimeke/FixerApiAcceptanceTests.git
   cd FixerApiAcceptanceTests
   ```

2. **Set your Fixer.io API key as environment variable**

   On **Linux/macOS**:
   ```bash
   export FIXER_API_KEY_FREE=your_api_key_here
   ```

   On **Windows (PowerShell)**:
   ```powershell
   setx FIXER_API_KEY_FREE "your_api_key_here"
   ```

   > ⚠️ Restart VS Code or your terminal after setting the variable.  
   > The free Fixer.io plan only supports `base=EUR` and requires HTTPS.

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

---

## ▶️ Running Tests

From the project root:

```bash
dotnet test
```

You will see output like:

```
[xUnit.net 00:00:00.52]     Scenario: Get latest rates with valid access key (positive) [PASS]
[xUnit.net 00:00:00.71]     Scenario: Invalid access key returns error (negative) [PASS]
```

---

## 📝 Logging

All requests and responses are written to `Report.log` in the project root:

> The API key is masked in logs (`abcd****`).  
> The file is included in `.gitignore` so it will not be committed to GitHub.

---

## ✅ Test Scenarios

### Positive
1. **Valid free key returns latest rates**  
   - Given a valid API key (free)  
   - When requesting `/latest`  
   - Then response is `success:true`  
   - Then response shows EUR as base currency

2. **Valid free key with symbols filter**  
   - Given a valid API key (free)  
   - When requesting `/latest?symbols=USD,DKK`  
   - Then response is `success:true`  
   - And only USD + DKK rates are returned  

### Negative
3. **Missing access key**  
   - Given no API key  
   - When requesting `/latest`  
   - Then response is `success:false`  
   - And error type is `missing_access_key`  

4. **Invalid access key**  
   - Given an invalid API key  
   - When requesting `/latest`  
   - Then response is `success:false`  
   - And error type is `invalid_access_key`  

---

## 🚀 Future Improvements

- Add more positive/negative scenarios (e.g., unsupported symbols, not allowed base currency, rate limits)  
- Using Specflow for more BDD/Gherkin 
- Structured JSON reporting instead of plain log file  
- Performance tests

---

