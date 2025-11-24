using Lacuna.Pki.Pades;

namespace Lacuna.Sign;

public static class PdfSignatureUtils {
   public static bool IsPdfDigitallySigned(string filePath) {
      if (string.IsNullOrWhiteSpace(filePath)) {
         return false;
      }
      if (!File.Exists(filePath)) {
         // File missing => treat as not signed (or handle differently if you prefer)
         return false;
      }
      var pdfBytes = File.ReadAllBytes(filePath);
      var signature = PadesSignature.Open(pdfBytes);
      if (signature.Signers.Any()) {
         return true;
      }
      return false;
   }
}