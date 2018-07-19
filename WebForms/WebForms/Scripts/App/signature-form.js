// ----------------------------------------------------------------------------------------------------------
// This file contains logic for calling the Web PKI component to perform a signature. It is only an example,
// feel free to alter it to meet your application's needs.
// ----------------------------------------------------------------------------------------------------------
var signatureForm = (function () {
    
    // Auxiliary global variables.
    var formElements = {};
	var selectedCertThumbprint = null;
	var pki = null;

	// ------------------------------------------------------------------------------------------------------
    // Function called once the page is loaded or once the update panel with the hidden fields used to pass
    // data to and from the code-behind is updated.
	// ------------------------------------------------------------------------------------------------------
	function pageLoad(fe) {

        // We update our references to the form elements everytime this function is called, since the
        // elements change when the UpdatePanel is updated.
		formElements = fe;

		if (pki === null) {
            // If the Web PKI component is not initialized that means this is the initial load of the page
            // (not a refresh of the update panel). Therefore, we initialize the Web PKI component and list
            // the user's certificates.
			initPki();
		} else if (formElements.toSignHashField.val()) {
            // If the Web PKI is already initialized, this is a refresh of the update panel. If the hidden
            // field "toSignHash" was filled on the code-behind, we go ahead and sign it.
			sign();
		} else {
            // If the Web PKI is already initialized but the hidden field "toSignHash" is empty, this is
            // a refresh of the update panel but the signature could not be initiated on the code-behind
            // (probably because of a validation error). Therefore, we just unblock the UI (which is was
            // previously blocked by the sign() function).
			$.unblockUI();
		}
	}

	// ------------------------------------------------------------------------------------------------------
	// Function that initializes the Web PKI component, called on the first load of the page.
	// ------------------------------------------------------------------------------------------------------
	function initPki() {

		// Block the UI while we get things ready.
		$.blockUI({ message: 'Initializing ...' });

		// Create an instance of the Lacuna object.
		pki = new LacunaWebPKI();

		// Call the init() method on the LacunaWebPKI object, passing a callback for when the component is
        // ready to be used and another to be called when an error occurrs on any of the subsequent
        // operations. For more information, see:
		// https://docs.lacunasoftware.com/en-us/articles/web-pki/get-started.html#coding-the-first-lines
		// http://webpki.lacunasoftware.com/Help/classes/LacunaWebPKI.html#method_init
		pki.init({
            ready: loadCertificates,    // As soon as the component is ready we'll load the certificates.
			defaultError: onWebPkiError // Generic error callback defined below.
		});
	}
	
	// ------------------------------------------------------------------------------------------------------
	// Function called when the user clicks the "Refresh" button.
	// ------------------------------------------------------------------------------------------------------
	function refresh() {
		// Block the UI while we load the certificates.
		$.blockUI();
		// Invoke the loading of the certificates.
		loadCertificates();
	}

	// ------------------------------------------------------------------------------------------------------
    // Function that loads the certificates, either on startup or when the user clicks the "Refresh" button.
    // At this point, the UI is already blocked.
	// ------------------------------------------------------------------------------------------------------
	function loadCertificates() {

		// Call the listCertificates() method to list the user's certificates. For more information see:
		// http://webpki.lacunasoftware.com/Help/classes/LacunaWebPKI.html#method_listCertificates
		pki.listCertificates({

			// The ID of the <select> element to be populated with the certificates.
			selectId: formElements.certificateSelect.attr('id'),

			// Function that will be called to get the text that should be displayed for each option.
			selectOptionFormatter: function (cert) {
                var s = cert.subjectName + ' (issued by ' + cert.issuerName + ')';
                if (new Date() > cert.validityEnd) {
                    s = '[EXPIRED] ' + s;
                }
                return s;
			}

		}).success(function () {

			// Once the certificates have been listed, unblock the UI.
			$.unblockUI();
		});
	}

	// ------------------------------------------------------------------------------------------------------
	// Function called when the user clicks the "Sign File" button.
	// ------------------------------------------------------------------------------------------------------
	function startSignature() {

		// Block the UI while we perform the signature.
		$.blockUI({ message: 'Signing ...' });

        // Get the value attribute of the option selected on the dropdown. Since we placed the "thumbprint"
        // property on the value attribute of each item (see function loadCertificates above), we're actually
        // retrieving the thumbprint of the selected certificate.
        selectedCertThumbprint = formElements.certificateSelect.val();

		// Read the selected certificate's encoding.
		pki.readCertificate(selectedCertThumbprint).success(function (certEncoded) {

			// Fill the hidden field "certificateField" with the certificate encoding
			formElements.certificateField.val(certEncoded);

            // Fire up the click event of the button "SubmitCertificateButton" on the page's code-behind
            // (server-side).
			formElements.submitCertificateButton.click();
		});
	}

	// ------------------------------------------------------------------------------------------------------
	// Function that signs "toSignHash" computed on the code-behind.
	// ------------------------------------------------------------------------------------------------------
    function sign() {

        // Call Web PKI passing the selected certificate, the document's "to sign hash" and the digest
        // algorithm to be used during the signature algorithm.
		pki.signHash({

			thumbprint: selectedCertThumbprint,
			hash: formElements.toSignHashField.val(),
			digestAlgorithm: formElements.digestAlgorithmField.val()

		}).success(function (signature) {

			// Fill the hidden field "signatureField" with the result of the signature algorithm.
			formElements.signatureField.val(signature);

            // Fire up the click event of the button "SubmitSignatureButton" on the code-behind
            // (server-side).
			formElements.submitSignatureButton.click();
		});
	}

	// ------------------------------------------------------------------------------------------------------
	// Function called if an error occurs on the Web PKI component.
	// ------------------------------------------------------------------------------------------------------
    function onWebPkiError(message, error, origin) {

		// Unblock the UI.
		$.unblockUI();
		// Log the error to the browser console (for debugging purposes).
		if (console) {
			console.log('An error has occurred on the signature browser component: ' + message, error);
		}
        // Show the message to the user. You might want to substitute the alert below with a more
        // user-friendly UI component to show the error.
		alert(message);
	}

	// ------------------------------------------------------------------------------------------------------
	// Handling of errors in the UpdatePanel refresh.
	// ------------------------------------------------------------------------------------------------------
	if (Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {
		Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function (sender, args) {
			if (args.get_error()) {
				alert('An error has occurred on the server');
				$.unblockUI();
			}
		});
	}

	return {
		pageLoad: pageLoad,
		refresh: refresh,
		startSignature: startSignature
    };

})();
