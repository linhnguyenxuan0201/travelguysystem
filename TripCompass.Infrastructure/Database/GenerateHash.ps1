# PowerShell script để generate password hash
# Chạy: .\GenerateHash.ps1 -Password "Admin@123"

param(
    [string]$Password = "Admin@123"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Generate Password Hash for TripCompass" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Tạo một file C# tạm thời để generate hash
$tempScript = @"
using Microsoft.AspNetCore.Identity;
using System;

var passwordHasher = new PasswordHasher<string>();
var password = "$Password";
var hash = passwordHasher.HashPassword(null!, password);

Console.WriteLine("Password: " + password);
Console.WriteLine("Hash: " + hash);
Console.WriteLine("");
Console.WriteLine("-- SQL Script:");
Console.WriteLine("UPDATE Users SET PasswordHash = '" + hash + "' WHERE Email = 'admin@tripcompass.com' OR UserName = 'admin';");
"@

$tempFile = [System.IO.Path]::GetTempFileName() + ".cs"
$tempScript | Out-File -FilePath $tempFile -Encoding UTF8

Write-Host "⚠️  Cần chạy ứng dụng để generate hash." -ForegroundColor Yellow
Write-Host ""
Write-Host "Cách 1: Chạy ứng dụng và truy cập:" -ForegroundColor Green
Write-Host "  http://localhost:5122/Setup/GenerateHash?password=$Password" -ForegroundColor White
Write-Host ""
Write-Host "Cách 2: Sử dụng endpoint TestPassword sau khi ứng dụng chạy:" -ForegroundColor Green
Write-Host "  http://localhost:5122/Account/TestPassword?email=admin@tripcompass.com&password=$Password" -ForegroundColor White
Write-Host ""

Remove-Item $tempFile -ErrorAction SilentlyContinue
