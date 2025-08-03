# Rate Limiting Test Script
# Sử dụng: .\test-rate-limit.ps1

param(
    [string]$BaseUrl = "https://localhost:7001",
    [int]$RequestCount = 15,
    [int]$DelayMs = 50
)

Write-Host "🚀 Rate Limiting Test Script" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Request Count: $RequestCount" -ForegroundColor Yellow
Write-Host "Delay: ${DelayMs}ms" -ForegroundColor Yellow
Write-Host ""

$successCount = 0
$errorCount = 0
$rateLimitCount = 0
$startTime = Get-Date

function Test-SingleRequest {
    param([string]$Url, [string]$Description)
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ SUCCESS: $Description" -ForegroundColor Green
            return $true
        } else {
            Write-Host "❌ ERROR: $Description (Status: $($response.StatusCode))" -ForegroundColor Red
            return $false
        }
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 429) {
            Write-Host "🚫 RATE LIMITED: $Description" -ForegroundColor Red
            return "rate_limited"
        } else {
            Write-Host "❌ EXCEPTION: $Description - $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }
}

function Test-RateLimit {
    Write-Host "🧪 Testing Rate Limiting..." -ForegroundColor Cyan
    
    for ($i = 1; $i -le $RequestCount; $i++) {
        $url = "$BaseUrl/Menu/Menu?id_table=B02&restaurant_id=CHA1001"
        $result = Test-SingleRequest -Url $url -Description "Request $i"
        
        switch ($result) {
            $true { $successCount++ }
            $false { $errorCount++ }
            "rate_limited" { $rateLimitCount++ }
        }
        
        if ($i -lt $RequestCount) {
            Start-Sleep -Milliseconds $DelayMs
        }
    }
}

function Test-BurstRequests {
    Write-Host "🧪 Testing Burst Requests..." -ForegroundColor Cyan
    
    $jobs = @()
    for ($i = 1; $i -le $RequestCount; $i++) {
        $url = "$BaseUrl/Menu/Menu?id_table=B02&restaurant_id=CHA1001"
        $jobs += Start-Job -ScriptBlock {
            param($Url, $Description)
            try {
                $response = Invoke-WebRequest -Uri $Url -Method GET -UseBasicParsing -TimeoutSec 10
                if ($response.StatusCode -eq 200) {
                    return "success"
                } else {
                    return "error"
                }
            }
            catch {
                if ($_.Exception.Response.StatusCode -eq 429) {
                    return "rate_limited"
                } else {
                    return "error"
                }
            }
        } -ArgumentList $url, "Burst Request $i"
    }
    
    $results = $jobs | Wait-Job | Receive-Job
    $jobs | Remove-Job
    
    foreach ($result in $results) {
        switch ($result) {
            "success" { $successCount++ }
            "error" { $errorCount++ }
            "rate_limited" { $rateLimitCount++ }
        }
    }
}

function Show-Summary {
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Host ""
    Write-Host "📊 Test Summary:" -ForegroundColor Yellow
    Write-Host "   Duration: $($duration.TotalSeconds.ToString('F2')) seconds" -ForegroundColor White
    Write-Host "   Total Requests: $RequestCount" -ForegroundColor White
    Write-Host "   Success: $successCount" -ForegroundColor Green
    Write-Host "   Errors: $errorCount" -ForegroundColor Red
    Write-Host "   Rate Limited: $rateLimitCount" -ForegroundColor Red
    
    if ($rateLimitCount -gt 0) {
        Write-Host "✅ Rate limiting is working!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  No rate limiting detected" -ForegroundColor Yellow
    }
}

# Main execution
Write-Host "1. Testing single request..." -ForegroundColor Cyan
Test-SingleRequest -Url "$BaseUrl/Menu/Menu?id_table=B02&restaurant_id=CHA1001" -Description "Single Request"

Write-Host ""
Write-Host "2. Testing sequential rate limiting..." -ForegroundColor Cyan
Test-RateLimit

Write-Host ""
Write-Host "3. Testing burst requests..." -ForegroundColor Cyan
Test-BurstRequests

Show-Summary

Write-Host ""
Write-Host "💡 Tips:" -ForegroundColor Cyan
Write-Host "   - Rate limit: 10 requests/second, 60 requests/minute" -ForegroundColor White
Write-Host "   - Check logs for detailed rate limiting info" -ForegroundColor White
Write-Host "   - Use different table IDs to test isolation" -ForegroundColor White 