using System;
using System.IO;
using System.Security.Cryptography;
using CommandLine;
using Lacuna.Pki;
using Lacuna.Pki.Cades;
using Lacuna.Pki.Stores;

namespace Lacuna.Signer;

public class Program {
	[Verb("list", HelpText = "List Certificates")]
	class ListOptions {
		//normal options here
	}
	[Verb("sign", HelpText = "sign file using detached CAdES")]
	class SignOptions {
		[Option('f', "file", Required = true, HelpText = "Input file to be signed.")]
		public string InputFile { get; set; } = "";

		[Option('s', "signedFile", Required = true, HelpText = "output signed file.")]
		public string SignedFile { get; set; } = "";

		[Option('c', "certificate", Required = true, HelpText = "certificate number")]
		public int Certificate { get; set; }

	}

	[Verb("validate", HelpText = "validate signed file")]
	class ValidadeOptions {
		[Option('f', "file", Required = true, HelpText = "Signed file to be validate.")]
		public string InputFile { get; set; } = "";

		[Option('s', "signature", Required = true, HelpText = "File thar contais CAdES signature. Usualy .p7s")]
		public string SignatureFile { get; set; } = "";
	}


	public static int Main(string[] args) {
		var license = "======= Ask for a license at support@lacunasoftware.com =========";
		if(license == "======= Ask for a license at support@lacunasoftware.com =========") {
			Console.WriteLine("License not set");
			return 0;
		}
		PkiConfig.LoadLicense(Convert.FromBase64String(license));

		return CommandLine.Parser.Default.ParseArguments<ListOptions, SignOptions, ValidadeOptions>(args)
		  .MapResult(
			 (ListOptions opts) => RunListAndReturnExitCode(opts),
			 (SignOptions opts) => RunSignAndReturnExitCode(opts),
			 (ValidadeOptions opts) => RunValidateAndReturnExitCode(opts),
			 errs => 1);

	}

	private static int RunValidateAndReturnExitCode(ValidadeOptions opts) {
		var cadesSignature = CadesSignature.Open(opts.SignatureFile);
		using var stream = File.OpenRead(opts.InputFile);
		var digestAlgorithm = DigestAlgorithm.SHA256;
		var digest = digestAlgorithm.ComputeHash(stream);
		cadesSignature.SetExternalDataDigest(digestAlgorithm, digest);
		var cadesSI = cadesSignature.Signers.First();
		var vr = cadesSignature.ValidateSignature(cadesSI, CadesPoliciesForValidation.GetPkiBrazil());
		if(vr.IsValid) {
			Console.WriteLine("");
			Console.WriteLine($"{opts.SignatureFile} is a valid signature for {opts.InputFile}");
			Console.WriteLine($"Hash algorithm SHA256 value : {String.Concat(digest.Select(b => b.ToString("X2"))) }");
			Console.WriteLine($"Signed by {cadesSI.SigningCertificate.SubjectDisplayName}");
			Console.WriteLine($"Signature date: {cadesSI.SigningTime:dd/MM/yyyy HH:mm:ss} GMT");
		} else {
			Console.WriteLine("");
			Console.WriteLine($"{opts.SignatureFile} is a invalid signature for {opts.InputFile}");
		}
		return 0;
	}

	private static int RunSignAndReturnExitCode(SignOptions opts) {
		var certificates = WindowsCertificateStore.LoadPersonalCurrentUser().GetCertificatesWithKey().Where(c => c.Certificate.PkiBrazil.CPF != null).ToList();
		var certificate = certificates[opts.Certificate];
		var fileName = opts.InputFile;
		using var stream = File.OpenRead(fileName);
		var digestAlgorithm = DigestAlgorithm.SHA256;
		var digest = digestAlgorithm.ComputeHash(stream);
		var signer = new CadesSigner();
		signer.SetSigningCertificate(certificate);
		signer.SetPolicy(CadesPoliciesForGeneration.GetPkiBrazilAdrBasica());
		signer.SetEncapsulatedContent(false);
		signer.SetDataDigestToSign(digestAlgorithm, digest);
		signer.ComputeSignature();

		var cades = signer.GetSignature();
		File.WriteAllBytes(opts.SignedFile, cades);
		stream.Close();
		return 0;
	}

	private static int RunListAndReturnExitCode(ListOptions opts) {
		var certificates = WindowsCertificateStore.LoadPersonalCurrentUser().GetCertificatesWithKey().Where(c => c.Certificate.PkiBrazil.CPF != null).ToList();
		for (int i = 0; i < certificates.Count; i++) {
			Console.WriteLine($"{i} - {certificates[i].Certificate.SubjectDisplayName}");
		}
		return 0;
	}
}

