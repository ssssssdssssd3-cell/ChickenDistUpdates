 = 'Server=.;Database=ChickenDistDB;Integrated Security=True;TrustServerCertificate=True;'
try {
     = New-Object System.Data.SqlClient.SqlConnection('Server=.;Database=ChickenDistDB;Integrated Security=True;TrustServerCertificate=True;')
    .Open()
    Write-Host 'Connected to default instance!'
} catch {
    try {
         = New-Object System.Data.SqlClient.SqlConnection('Server=.\SQLEXPRESS;Database=ChickenDistDB;Integrated Security=True;TrustServerCertificate=True;')
        .Open()
        Write-Host 'Connected to SQLEXPRESS!'
    } catch {
        Write-Host 'Error:' .Exception.Message
    }
}
if (.State -eq 'Open') {
     = .CreateCommand()
    .CommandText = 'SELECT TOP 5 * FROM Shifts ORDER BY ShiftID DESC'
     = New-Object System.Data.SqlClient.SqlDataAdapter()
     = New-Object System.Data.DataTable
    .Fill() | Out-Null
     | Format-Table -AutoSize | Out-String | Write-Host
}
