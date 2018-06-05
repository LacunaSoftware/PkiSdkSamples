using System;

namespace SimpleMVC.Models {
	public class SignatureCompleteMVCModel {

		public byte[] CertContent { get; set; }
		public string CertContentBase64 {
			get {
				return CertContent != null ? Convert.ToBase64String(CertContent) : "";
			}
			set {
				CertContent = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
			}
		}

		//public byte[] ToSignBytes { get; set; }
		//public string ToSignBytesBase64 {
		//	get {
		//		return ToSignBytes != null ? Convert.ToBase64String(ToSignBytes) : "";
		//	}
		//	set {
		//		ToSignBytes = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
		//	}
		//}

		//public byte[] ToSignHash { get; set; }
		//public string ToSignHashBase64 {
		//	get {
		//		return ToSignHash != null ? Convert.ToBase64String(ToSignHash) : "";
		//	}
		//	set {
		//		ToSignHash = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
		//	}
		//}

		public string TransferDataFileId { get; set; }

		public string DigestAlgorithmOid { get; set; }

		public byte[] Signature { get; set; }
		public string SignatureBase64 {
			get {
				return Signature != null ? Convert.ToBase64String(Signature) : "";
			}
			set {
				Signature = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
			}
		}

	}
}