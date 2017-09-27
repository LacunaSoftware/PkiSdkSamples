using Lacuna.Pki;
using Lacuna.Pki.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC.Models {
    public class XmlSignatureModel {
        public XmlSignature Signature { get; set; }
        public ValidationResults ValidationResults { get; set; }
    }
}