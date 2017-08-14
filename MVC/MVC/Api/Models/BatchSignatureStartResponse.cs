using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC.Api.Models {
    public class BatchSignatureStartResponse {

        public byte[] TransferData { get; set; }
        public string TransferDataBase64 {
            get {
                return TransferData != null ? Convert.ToBase64String(TransferData) : "";
            }
            set {
                TransferData = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
            }
        }

        public byte[] ToSignHash { get; set; }
        public string ToSignHashBase64 {
            get {
                return ToSignHash != null ? Convert.ToBase64String(ToSignHash) : "";
            }
            set {
                ToSignHash = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
            }
        }

        public string DigestAlgorithmOid { get; set; }
    }
}