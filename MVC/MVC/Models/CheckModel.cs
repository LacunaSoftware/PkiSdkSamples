using Lacuna.Pki.Pades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC.Models {
    public class CheckModel {
        public PadesSignature Signature { get; set; }
        public string FileId { get; set; }
    }
}