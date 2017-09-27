using Lacuna.Pki;
using Lacuna.Pki.Pades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC.Models {
    public class PadesSignatureModel {
        public PadesSignerInfo Signer { get; set; }
        public ValidationResults ValidationResults { get; set; }
    }
}