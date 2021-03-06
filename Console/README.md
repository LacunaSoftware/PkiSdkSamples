Lacuna PKI SDK sample console application
========================================

This folder contains a sample console application that shows how to use the
[Lacuna PKI SDK](https://www.lacunasoftware.com/en/products/pki_sdk).
This sample also presents the inclusion of metadata in accordance with Brazilian Decree nº 10.278/2020.

For other technologies, please visit the [repository root](https://github.com/LacunaSoftware/PkiSdkSamples).

To run the samples, you will need a license. If you don't have one, request a trial license at
[our website](http://www.lacunasoftware.com/en/home/contact).


Running the project
-------------------

To run the project:

1. [Download](https://github.com/LacunaSoftware/PkiSdkSamples/archive/master.zip) or clone the repository

2. Open the solution file `Console\ConsoleApp.sln` on Visual Studio

3. On the TXT file of your PKI SDK license (`LacunaPkiLicense.txt`), locate the **base64-encoded binary license**, e.g.:
	
	```
	Binary license content (Base64-encoded)
	---------------------------------------
	AhAAebt1gVE2NEe+N+nchF42UVwAQlJJU0EgU09DSU...
	```

4. Paste the base64-encoded binary license on the file `Console\ConsoleApp\Program.cs`, e.g.:

	```
	// ---------------------------------------------------------------------------------------
	// SET YOUR BINARY LICENSE BELOW
	private const string LicenseBase64 = "AhAAebt1gVE2NEe+N+nchF42UVwAQlJJU0EgU09DSU...";
	//                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
	// ---------------------------------------------------------------------------------------
	```

5. Run the solution. Make sure your system allows automatic Nuget package restore (if it doesn't, manually restore the packages).

See Also
--------

* [Sample projects with other .NET technologies](https://github.com/LacunaSoftware/PkiSdkSamples)
* [Online documentation](http://pki.lacunasoftware.com/Help)
* [Lacuna PKI SDK package on Nuget](https://www.nuget.org/packages/Lacuna.Pki)
* [Test certificates](../TestCertificates.md)
