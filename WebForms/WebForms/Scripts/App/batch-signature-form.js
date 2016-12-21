var batchSignatureForm = (function () {

	var pki = null;
	var formElements = {};
	var docCount = -1;

	// -------------------------------------------------------------------------------------------------
	// Function called once the page is loaded
	// -------------------------------------------------------------------------------------------------
	function pageLoad(fe) {

		formElements = fe;

		var toSignHash = formElements.toSignHashField.val();
		if (toSignHash) {
			if (toSignHash === '(end)') {
				$.unblockUI();
			} else {
				sign();
			}
			return;
		}

		// Block the UI while we get things ready
		$.blockUI({ message: 'Inicializando ...' });

		pki = new LacunaWebPKI();

		// Call the init() method on the LacunaWebPKI object, passing a callback for when
		// the component is ready to be used and another to be called when an error occurrs
		// on any of the subsequent operations. For more information, see:
		// https://webpki.lacunasoftware.com/#/Documentation#coding-the-first-lines
		// http://webpki.lacunasoftware.com/Help/classes/LacunaWebPKI.html#method_init
		pki.init({
			ready: loadCertificates,
			defaultError: onWebPkiError // generic error callback on Content/js/app/site.js
		});
	}

	function setDocumentCount(count) {
		docCount = count;
	};

	// -------------------------------------------------------------------------------------------------
	// Function called when the user clicks the "Refresh" button
	// -------------------------------------------------------------------------------------------------
	function refresh() {
		// Block the UI while we load the certificates
		$.blockUI();
		// Invoke the loading of the certificates
		loadCertificates();
	}

	// -------------------------------------------------------------------------------------------------
	// Function that loads the certificates, either on startup or when the user
	// clicks the "Refresh" button. At this point, the UI is already blocked.
	// -------------------------------------------------------------------------------------------------
	function loadCertificates() {

		// Call the listCertificates() method to list the user's certificates
		pki.listCertificates({

			// specify that expired certificates should be ignored
			//filter: pki.filters.isWithinValidity,

			// in order to list only certificates within validity period and having a CPF (ICP-Brasil), use this instead:
			//filter: pki.filters.all(pki.filters.hasPkiBrazilCpf, pki.filters.isWithinValidity),

			// id of the select to be populated with the certificates
			selectId: formElements.certificateSelect.attr('id'),

			// function that will be called to get the text that should be displayed for each option
			selectOptionFormatter: function (cert) {
				return cert.subjectName + ' (validade: ' + cert.validityEnd.toDateString() + ', emissor: ' + cert.issuerName + ')';
			}

		}).success(function () {

			// once the certificates have been listed, unblock the UI
			$.unblockUI();
		});
	}

	// -------------------------------------------------------------------------------------------------
	// Function called when the user clicks the "Sign" button
	// -------------------------------------------------------------------------------------------------
	function start() {

		// Block the UI while we perform the signature
		$.blockUI({ message: 'Assinando ...' });

		// Get the thumbprint of the selected certificate
		var selectedCertThumbprint = formElements.certificateSelect.val();
		formElements.certThumbField.val(selectedCertThumbprint);

		// Call Web PKI to preauthorize the signatures, so that the user only sees one confirmation dialog
		pki.preauthorizeSignatures({
			certificateThumbprint: selectedCertThumbprint,
			signatureCount: docCount // number of signatures to be authorized by the user
		}).success(function () {
			pki.readCertificate(selectedCertThumbprint).success(function (certEncoded) {
				formElements.certContentField.val(certEncoded);
				formElements.submitCertificateButton.click();
			});
		});
	}

	function sign() {
		pki.signHash({
			thumbprint: formElements.certThumbField.val(),
			hash: formElements.toSignHashField.val(),
			digestAlgorithm: formElements.digestAlgorithmField.val()
		}).success(function (signature) {
			formElements.signatureField.val(signature);
			formElements.submitSignatureButton.click();
		});
	}

	// -------------------------------------------------------------------------------------------------
	// Function called if an error occurs on the Web PKI component
	// -------------------------------------------------------------------------------------------------
	function onWebPkiError(message, error, origin) {
		// Unblock the UI
		$.unblockUI();
		// Log the error to the browser console (for debugging purposes)
		if (console) {
			console.log('An error has occurred on the signature browser component: ' + message, error);
		}
		// Show the message to the user. You might want to substitute the alert below with a more user-friendly UI
		// component to show the error.
		alert(message);
	}

	return {
		setDocumentCount: setDocumentCount,
		pageLoad: pageLoad,
		refresh: refresh,
		start: start
	};
})();