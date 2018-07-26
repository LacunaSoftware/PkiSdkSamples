using Lacuna.Pki.Asn1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWpfApp {

	/**
	 * LacunaHolderPhotoAttribute ::= SEQUENCE {
	 *		version    Version,
	 *		mimeType   IA5String,
	 *		content    OCTET STRING
	 * }
	 * 
	 * Version ::= INTEGER { v1(0) }
	 */
	[Asn1Sequence]
	class LacunaHolderPhotoAttribute {

		public const string Oid = "1.3.6.1.4.1.46332.1.1";

		[Asn1SequenceElement(0, Asn1PrimitiveTypes.Integer)]
		public int Version => 0;

		[Asn1SequenceElement(1, Asn1PrimitiveTypes.IA5String)]
		public string MimeType { get; set; }

		[Asn1SequenceElement(2, Asn1PrimitiveTypes.OctetString)]
		public byte[] Content { get; set; }
	}
}
