using System;

namespace WebForms.Models {
	public class BatchSignatureStartRequest {
		public int Id { get; set; }
		public byte[] CertContent { get; set; }
		public string CertContentBase64
		{
			get { return CertContent != null ? Convert.ToBase64String(CertContent) : ""; }
			set { CertContent = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null; }
		}
	}
}