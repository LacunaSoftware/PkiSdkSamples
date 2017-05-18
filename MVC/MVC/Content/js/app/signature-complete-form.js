var signatureCompleteForm = (function () {

	var pki = null;
	var formElements = {};

	// -------------------------------------------------------------------------------------------------
	// Function called once the page is loaded
	// -------------------------------------------------------------------------------------------------
	function init(fe) {

		formElements = fe;

		if (!formElements.formIsValid) {
			formElements.tryAgainButton.show();
			return;
		}

		// Block the UI while we get things ready
		$.blockUI({ message: 'Signing ...' });

		pki = new LacunaWebPKI();

		// Call the init() method on the LacunaWebPKI object, passing a callback for when
		// the component is ready to be used and another to be called when an error occurrs
		// on any of the subsequent operations. For more information, see:
		// https://webpki.lacunasoftware.com/#/Documentation#coding-the-first-lines
		// http://webpki.lacunasoftware.com/Help/classes/LacunaWebPKI.html#method_init
		pki.init({
			ready: sign, // as soon as the component is ready we'll load the certificates
			defaultError: onWebPkiError // generic error callback on Content/js/app/site.js
		});
	}

	function sign() {
		pki.signHash({
			thumbprint: formElements.certThumbField.val(),
			hash: formElements.toSignHashField.val(),
			digestAlgorithm: formElements.digestAlgorithmOidField.val()
		}).success(function (signature) {
			formElements.signatureField.val(signature);
			formElements.form.submit();
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

		formElements.tryAgainButton.show();
	}

	return {
		init: init
	};

})();
