# SPOCS AI Shopping Assistant — Phase 5 Automated Demo Runner
param(
    [string]$BaseUrl = "http://localhost:5107"
)

$scenarios = @(
    @{
        Id = "1"
        Name = "Product Discovery (Battery & Travel)"
        Query = 'I need a laptop with long battery life for traveling and remote work.'
    },
    @{
        Id = "2"
        Name = "Natural-Language Spec Search (120Hz Display)"
        Query = 'Which devices in your store have a 120Hz or higher refresh rate display?'
    },
    @{
        Id = "3"
        Name = "Strict Price Constraint ($400 Phone)"
        Query = 'I have a strict budget of $400 for a smartphone with good battery.'
    },
    @{
        Id = "4"
        Name = "Feature Requirements (Outdoor Diving Smartwatch)"
        Query = 'Show me water-resistant smartwatches with GPS for outdoor sports and diving.'
    },
    @{
        Id = "5"
        Name = "Product Comparison (S24 Ultra vs Pixel 9 Pro)"
        Query = 'Compare the Samsung Galaxy S24 Ultra and the Google Pixel 9 Pro. Which one has better zoom?'
    },
    @{
        Id = "6"
        Name = "No Matching Products (Budget Constraint Exceeded)"
        Query = 'Find me a dedicated gaming laptop under $500.'
    },
    @{
        Id = "7"
        Name = "Ambiguous Question"
        Query = 'I want something nice.'
    },
    @{
        Id = "8"
        Name = "Unrelated Question (General Knowledge)"
        Query = 'What is the capital of France?'
    },
    @{
        Id = "9"
        Name = "Hallucination Prevention (Out-of-Catalog Item)"
        Query = 'Do you have the iPhone 16 Pro Max or PlayStation 5 in stock?'
    }
)

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "       SPOCS AI SHOPPING ASSISTANT - DEMO RUNNER        " -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

$passed = 0
$failed = 0

foreach ($s in $scenarios) {
    Write-Host "--------------------------------------------------------" -ForegroundColor Yellow
    Write-Host "[$($s.Id)/9] SCENARIO: $($s.Name)" -ForegroundColor Yellow
    Write-Host "CUSTOMER QUERY: '$($s.Query)'" -ForegroundColor White

    $payload = @{
        message = $s.Query
        topK = 3
    } | ConvertTo-Json

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/chat" `
            -Method Post `
            -ContentType "application/json" `
            -Body $payload `
            -TimeoutSec 45

        $stopwatch.Stop()
        $elapsed = [math]::Round($stopwatch.Elapsed.TotalSeconds, 2)

        Write-Host "`nAI ASSISTANT REPLY ($elapsed s, Model: $($response.model)):" -ForegroundColor Green
        Write-Host $response.reply -ForegroundColor Gray

        if ($response.referencedProducts -and $response.referencedProducts.Count -gt 0) {
            Write-Host "`nREFERENCED PRODUCTS ($($response.referencedProducts.Count)):" -ForegroundColor DarkCyan
            foreach ($p in $response.referencedProducts) {
                $price = $p.basePrice
                $name = $p.name
                $stock = $p.inStock
                Write-Host "  - $name | `$$price | InStock: $stock" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "`nREFERENCED PRODUCTS: (None - Direct Conversational Reply)" -ForegroundColor DarkGray
        }

        $passed++
    } catch {
        $stopwatch.Stop()
        Write-Host "`nFAILED: $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }

    Write-Host ""
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "DEMO SUMMARY: Total=$($scenarios.Count) | Passed=$passed | Failed=$failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "========================================================`n" -ForegroundColor Cyan
