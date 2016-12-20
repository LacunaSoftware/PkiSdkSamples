'use strict';
app.controller('padesSignatureController', ['$scope', '$http', 'blockUI', 'util', function ($scope, $http, blockUI, util) {

	var pki = new LacunaWebPKI();

	var init = function () {

		blockUI.start();

		pki.init({
			ready: loadCertificates,
			defaultError: onWebPkiError,
			angularScope: $scope
		});

	};

	$scope.refresh = function () {
		blockUI.start();
		loadCertificates();
	};

	var loadCertificates = function () {

		// Call the listCertificates() method to list the user's certificates. For more information see
		// http://webpki.lacunasoftware.com/Help/classes/LacunaWebPKI.html#method_listCertificates
		pki.listCertificates({

			// specify that expired certificates should be ignored
			//filter: pki.filters.isWithinValidity,

			// in order to list only certificates within validity period and having a CPF (ICP-Brasil), use this instead:
			//filter: pki.filters.all(pki.filters.hasPkiBrazilCpf, pki.filters.isWithinValidity),

		}).success(function (certificates) {

			// Remember the selected certificate (see below)
			var originalSelected = ($scope.selectedCertificate || {}).thumbprint;

			// Set available certificates on scope
			$scope.certificates = certificates;

			// Recover previous selection
			angular.forEach(certificates, function (c) {
				if (c.thumbprint === originalSelected) {
					$scope.selectedCertificate = c;
				}
			});

			// once the certificates have been listed, unblock the UI
			blockUI.stop();

		});
	};

	$scope.getCertificateDisplayName = function (cert) {
		return cert.subjectName + ' (expires on ' + cert.validityEnd.toDateString() + ', issued by ' + cert.issuerName + ')';
	};

	$scope.sign = function () {
		if ($scope.selectedCertificate == null) {
			util.showMessage('Message', 'Please select a certificate');
			return;
		}

		blockUI.start();

		// Call readCertificate() on the LacunaWebPKI object passing the selected certificate's thumbprint. This
		// reads the certificate's encoding, which we'll need later on. For more information, see
		// http://webpki.lacunasoftware.com/Help/classes/LacunaWebPKI.html#method_readCertificate
		pki.readCertificate($scope.selectedCertificate.thumbprint).success(onCertificateRetrieved);
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the user's certificate encoding has been read
	// -------------------------------------------------------------------------------------------------
	var onCertificateRetrieved = function (cert) {
		$http.post('Api/PadesSignature/Start', {
			certificate: cert
		}).then(function (response) {
			onSignatureStartCompleted(cert, response.data);
		}, util.handleServerError);
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the server replies with the "to-sign-bytes"
	// -------------------------------------------------------------------------------------------------
	var onSignatureStartCompleted = function (certificate, response) {
		pki.signData({
			thumbprint: $scope.selectedCertificate.thumbprint,
			data: response.toSignBytes,
			digestAlgorithm: response.digestAlgorithmOid
		}).success(function (signature) {
			onSignDataCompleted(response, certificate, signature);
		});
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the signature of the "to-sign-bytes" is completed
	// -------------------------------------------------------------------------------------------------
	var onSignDataCompleted = function (startResponse, cert, sign) {
		$http.post('Api/PadesSignature/Complete', {
			certificate: cert,
			signature: sign,
			toSignBytes: startResponse.toSignBytes,
			transferData: startResponse.transferData
		}).then(onSignatureCompleteCompleted, util.handleServerError);
	};

	var onSignatureCompleteCompleted = function (response) {
		blockUI.stop();

		util.showMessage('Signature completed successfully!', 'Click OK to see details').result.then(function () {
			util.showSignatureResults(response.data);
		});
	};

	var onWebPkiError = function (message, error, origin) {
		// Unblock the UI
		blockUI.stop();
		// Log the error to the browser console (for debugging purposes)
		if (console) {
			console.log('An error has occurred on the signature browser component: ' + message, error);
		}
		// Show the message to the user
		util.showMessage('Error', message);
	};

	init();

}]);
