// -------------------------------------------------------------------------------------------------
// Javascript module "batch signature form", used by the form BatchSignature.aspx
// -------------------------------------------------------------------------------------------------
var batchSignatureForm = (function () {

	var pki = null;
	var formElements = {};
	var docCount = -1;
	var selectedCertThumbprint = null;

	// -------------------------------------------------------------------------------------------------
	// Function called once the page is loaded or once the update panel with the hidden fields used to
	// pass data to and from the code-behind is updated
	// -------------------------------------------------------------------------------------------------
	function pageLoad(fe) {

		// We save the references to the form elements everytime this function is called, since the elements
		// change when the UpdatePanel is updated
		formElements = fe;

		// We inspect the value of the toSignHashField hidden field to determine on which state we are
		var toSignHash = formElements.toSignHashField.val();
		if (toSignHash) {
			if (toSignHash === '(end)') {
				// If the toSignHashField is filled with the value "(end)", it means that the last document in the
				// batch was processed. We simply unblock the UI and return.
				$.unblockUI();
			} else {
				// If the toSignHashField is filled with any other value (but is not empty), we are in the middle
				// of signing the batch's documents. That means we have already initialized the Web PKI component.
				// We skip right to the sign() function, which signs the current batch document
				sign();
			}
			return;
		}
		// If the toSignHashField is empty, we are at the very beginning of the process, and we need to initialize
		// the Web PKI component and list user's certificates

		// Block the UI while we get things ready
		$.blockUI({ message: 'Initializing ...' });

		// Create an instance of the LacunaWebPKI "object"
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

	// -------------------------------------------------------------------------------------------------
	// Function called by a inline javascript on the BatchSignature.aspx file informing the number of
	// documents in the batch
	// -------------------------------------------------------------------------------------------------
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
				return cert.subjectName + ' (expires on ' + cert.validityEnd.toDateString() + ', issued by ' + cert.issuerName + ')';
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
		$.blockUI({ message: 'Signing ...' });

		// Get the thumbprint of the selected certificate
		selectedCertThumbprint = formElements.certificateSelect.val();

		// Call Web PKI to preauthorize the signatures, so that the user only sees one confirmation dialog
		pki.preauthorizeSignatures({

			certificateThumbprint: selectedCertThumbprint,
			signatureCount: docCount // number of signatures to be authorized by the user

		}).success(function () {

			// Read the selected certificate's encoding
			pki.readCertificate(selectedCertThumbprint).success(function (certEncoded) {

				// Fill the hidden field "certContentField" with the certificate encoding
				formElements.certContentField.val(certEncoded);

				// Fire up the click event of the button "SubmitCertificateButton" on BatchSignature.aspx's code-behind (server-side)
				formElements.submitCertificateButton.click();

			});
		});
	}

	// -------------------------------------------------------------------------------------------------
	// Function that signs the current document's "to sign hash" using the selected certificate
	// -------------------------------------------------------------------------------------------------
	function sign() {

		// Call Web PKI passing the selected certificate, the document's "to sign hash" and the digest algorithm to be used
		// during the signature algorithm
		pki.signHash({

			thumbprint: selectedCertThumbprint,
			hash: formElements.toSignHashField.val(),
			digestAlgorithm: formElements.digestAlgorithmField.val()

		}).success(function (signature) {

			// Fill the hidden field "signatureField" with the result of the signature algorithm
			formElements.signatureField.val(signature);

			// Fire up the click event of the button "SubmitSignatureButton" on BatchSignature.aspx's code-behind (server-side)
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