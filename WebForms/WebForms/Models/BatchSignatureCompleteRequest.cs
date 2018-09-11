using System;

namespace WebForms.Models {
	public class BatchSignatureCompleteRequest {
		public byte[] Signature { get; set; }
		public string SignatureBase64
		{
			get { return Signature != null ? Convert.ToBase64String(Signature) : ""; }
			set { Signature = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null; }
		}

		public string TransferDataFileId { get; set; }
	}
}