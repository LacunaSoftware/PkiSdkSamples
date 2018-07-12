// ----------------------------------------------------------------------------------------------------------
// This file contains logic for calling the Web PKI component to perform a certificate authentication. It is
// only an example, feel free to alter it to meet your application's needs.
// ----------------------------------------------------------------------------------------------------------
var authenticationForm = (function () {

    // Auxiliary global variable.
    var formElements = {};

    // Create an instance of the Lacuna object.
    var pki = new LacunaWebPKI();

	// ------------------------------------------------------------------------------------------------------
    // Function called once the page is loaded.
	// ------------------------------------------------------------------------------------------------------
	function pageLoad(fe) {

        // Receive from parameters received as arguments.
		formElements = fe;

		// Block the UI while we get things ready.
		$.blockUI({ message: 'Initializing ...' });

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
	// Function called when the user clicks the "Sign In" button.
	// ------------------------------------------------------------------------------------------------------
    function signIn() {

		// Block the UI while we perform the signature.
		$.blockUI({ message: 'Signing in ...' });

		// Get the value attribute of the option selected on the dropdown. Since we placed the "thumbprint"
		// property on the value attribute of each item (see function loadCertificates above), we're actually
		// retrieving the thumbprint of the selected certificate.
		var selectedCertThumbprint = formElements.certificateSelect.val();

        pki.readCertificate(selectedCertThumbprint).success(function (certEncoded) {

			formElements.certificateField.val(certEncoded);
			pki.signData({
				thumbprint: selectedCertThumbprint,
				data: formElements.nonceField.val(),
				digestAlgorithm: formElements.digestAlgorithmField.val()
			}).success(function (signature) {
				formElements.signatureField.val(signature);
				formElements.submitButton.click();
			});

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

	return {
		pageLoad: pageLoad,
		signIn: signIn,
		refresh: refresh
    };

})();
