Test certificates
=================

If you need test certificates to use in a development or staging environment, you can
use one of the certificates in our test PKI.

**NOTICE: The Lacuna Test PKI should never be trusted in a production environment**

Download the file [TestCertificates.zip](TestCertificates.zip?raw=true) to get the certificates. All files are PKCS #12 certificates with password **1234**. The following certificates are included:

* Alan Mathison Turing
    * Email: testturing@lacunasoftware.com
    * ICP-Brasil mock certificate with CPF 56072386105
* Ferdinand Georg Frobenius
    * Email: testfrobenius@lacunasoftware.com
    * ICP-Brasil mock certificate with CPF 87378011126
* Pierre de Fermat
    * Email: test@lacunasoftware.com
    * ICP-Brasil mock certificate with CPF 47363361886

If you need a certificate with a particular information, [contact us](http://support.lacunasoftware.com/).

Always remember to **remove the trust in the Lacuna Test PKI root CA when you're moving to a production environment** (see the method Util.GetTrustArbitrator() from the sample project you are using).
