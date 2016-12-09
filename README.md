Lacuna PKI SDK Samples
======================

This project contains sample applications demonstrating the use of the [Lacuna PKI SDK](https://www.lacunasoftware.com/en/products/pki_sdk)
with different .NET technologies.

To run the samples, you will need a license. If you don't have one, request a trial license at
[our website](http://www.lacunasoftware.com/en/home/contact).

The following sample projects are available:

* [ASP.NET MVC](MVC/)

Coming soon:

* WebForms
* Web API / SPA

Test certificates
-----------------

If you need test certificates to use in a development or staging environment, you can
use one of the certificates in our test PKI.

**NOTICE: The Lacuna Test PKI should never be trusted in a production environment**

Download the file [TestCertificates.zip](TestCertificates.zip) to get the certificates. All files are PKCS #12 certificates with password **1234**. The following certificates are included:

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

Always remember to **remove the trust in the Lacuna Test PKI security context when you're moving to a production environment**. Better yet, use some sort of conditional compilation so that the test PKI is only trusted when running in debug mode.

See Also
--------

* [Online documentation](http://pki.lacunasoftware.com/Help)
* [Lacuna PKI SDK package on Nuget](https://www.nuget.org/packages/Lacuna.Pki)
