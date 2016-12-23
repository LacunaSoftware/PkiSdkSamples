﻿'use strict';
app.controller('xmlElementSignatureController', ['$scope', '$http', 'blockUI', 'util', function ($scope, $http, blockUI, util) {

	var pki = new LacunaWebPKI();

	// -------------------------------------------------------------------------------------------------
	// Function that initializes the Web PKI component
	// -------------------------------------------------------------------------------------------------
	var init = function () {

		blockUI.start();

		pki.init({
			ready: loadCertificates,
			defaultError: onWebPkiError,
			angularScope: $scope
		});

	};

	// -------------------------------------------------------------------------------------------------
	// Function called when the user clicks the "Refresh" button
	// -------------------------------------------------------------------------------------------------
	$scope.refresh = function () {
		blockUI.start();
		loadCertificates();
	};

	// -------------------------------------------------------------------------------------------------
	// Function that loads the certificates, either on startup or when the user
	// clicks the "Refresh" button. At this point, the UI is already blocked.
	// -------------------------------------------------------------------------------------------------
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

	// -------------------------------------------------------------------------------------------------
	// Function called when the user clicks the "Sign" button
	// -------------------------------------------------------------------------------------------------
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
		$http.post('Api/XmlElementSignature/Start', {
			certificate: cert
		}).then(function (response) {
			onSignatureStartCompleted(cert, response.data);
		}, util.handleServerError);
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the server replies with the "to-sign-hash"
	// -------------------------------------------------------------------------------------------------
	var onSignatureStartCompleted = function (certificate, startResponse) {
		pki.signHash({
			thumbprint: $scope.selectedCertificate.thumbprint,
			hash: startResponse.toSignHash,
			digestAlgorithm: startResponse.digestAlgorithmOid
		}).success(function (signature) {
			onSignDataCompleted(startResponse.transferData, certificate, signature);
		});
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the signature of the "to-sign-bytes" is completed
	// -------------------------------------------------------------------------------------------------
	var onSignDataCompleted = function (transferData, cert, sign) {
		$http.post('Api/XmlElementSignature/Complete', {
			certificate: cert,
			signature: sign,
			transferData: transferData
		}).then(onSignatureCompleteCompleted, util.handleServerError);
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the server replies with the signature filename and 
	// the certificate of who signed it.
	// -------------------------------------------------------------------------------------------------
	var onSignatureCompleteCompleted = function (completeResponse) {
		blockUI.stop();

		util.showMessage('Signature completed successfully!', 'Click OK to see details').result.then(function () {
			util.showSignatureResults(completeResponse.data);
		});
	};

	// -------------------------------------------------------------------------------------------------
	// Function called if an error occurs on the Web PKI component
	// -------------------------------------------------------------------------------------------------
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
