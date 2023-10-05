# Define the destination folders where you want to delete files
$destinationFolder1 = "C:\Temp\AutoSigner\Out\47891178631\"
$destinationFolder2 = "C:\Temp\AutoSigner\Out\67364448373\"

# Define the naming pattern of the files to be deleted (e.g., "CopyXX.ext")
$pattern = "Copy*.pdf"

# Delete files from the first folder
Remove-Item -Path (Join-Path -Path $destinationFolder1 -ChildPath $pattern) -Force

# Delete files from the second folder
Remove-Item -Path (Join-Path -Path $destinationFolder2 -ChildPath $pattern) -Force
