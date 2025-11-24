using CsvHelper.Configuration.Attributes;

namespace Lacuna.Sign;

public class SigningRecord {
   public string FileToSign { get; set; } = string.Empty;

   public string Certificate { get; set; } = string.Empty;

   public string SignatureDate { get; set; } = string.Empty;

   // "Left" or "Right"
   public string Position { get; set; } = string.Empty;
}