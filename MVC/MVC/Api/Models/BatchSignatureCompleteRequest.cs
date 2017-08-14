using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC.Api.Models {
    public class BatchSignatureCompleteRequest {
        public byte[] Signature { get; set; }
        public string SignatureBase64 {
            get {
                return Signature != null ? Convert.ToBase64String(Signature) : "";
            }
            set {
                Signature = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
            }
        }

        public byte[] TransferData { get; set; }
        public string TransferDataBase64 {
            get {
                return TransferData != null ? Convert.ToBase64String(TransferData) : "";
            }
            set {
                TransferData = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
            }
        }
    }
}