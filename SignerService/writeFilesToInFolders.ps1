# Define the source file to be copied
$sourceFile = "C:\Temp\AutoSigner\00.pdf"

# Define the destination folders
$destinationFolder1 = "C:\Temp\AutoSigner\In\47891178631\"
$destinationFolder2 = "C:\Temp\AutoSigner\In\67364448373\"

# Number of copies to create
$numberOfCopies = 500

# Loop to copy the file and transfer to both folders
for ($i = 1; $i -le $numberOfCopies; $i++) {
    $copyNumber = $i.ToString("D2")  # Format the copy number with leading zeros
    $destinationFile1 = Join-Path -Path $destinationFolder1 -ChildPath "Copy$copyNumber.pdf"
    $destinationFile2 = Join-Path -Path $destinationFolder2 -ChildPath "Copy$copyNumber.pdf"
     
    # Copy the file to the first folder
    Copy-Item -Path $sourceFile -Destination $destinationFile1

    # Copy the file to the second folder
    Copy-Item -Path $sourceFile -Destination $destinationFile2
}
