using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models {
	public class SignatureCompleteRequest {
		public string FileId { get; set; }
		public byte[] Certificate { get; set; }
		public byte[] Signature { get; set; }
		public byte[] ToSignBytes { get; set; }
		public byte[] TransferData { get; set; }
		public string TransferDataFileId { get; set; }
	}
}