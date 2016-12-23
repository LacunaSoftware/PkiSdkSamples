﻿'use strict';
app.controller('authenticationController', ['$scope', '$http', 'blockUI', 'util', function ($scope, $http, blockUI, util) {

	$scope.certificates = [];
	$scope.selectedCertificate = null;

	// Create an instance of the LacunaWebPKI "object"
	var pki = new LacunaWebPKI();

	// -------------------------------------------------------------------------------------------------
	// Function that initializes the Web PKI component
	// -------------------------------------------------------------------------------------------------
	var init = function () {

		// Block the UI while we get things ready
		blockUI.start();

		// Call the init() method on the LacunaWebPKI object, passing a callback for when
		// the component is ready to be used and another to be called when an error occurrs
		// on any of the subsequent operations. For more information, see:
		// https://webpki.lacunasoftware.com/#/Documentation#coding-the-first-lines
		// http://webpki.lacunasoftware.com/Help/classes/LacunaWebPKI.html#method_init
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
	// Function called when the user clicks the "Sign In" button
	// -------------------------------------------------------------------------------------------------
	$scope.signIn = function () {
		if ($scope.selectedCertificate == null) {
			util.showMessage('Message', 'Please select a certificate');
			return;
		}
		blockUI.start();
		$http.get('/Api/Authentication').then(onNonceAcquired, util.handleServerError);
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the server replies with the "nonce" data for the authentication
	// -------------------------------------------------------------------------------------------------
	var onNonceAcquired = function (response) {
		var nonce = response.data;

		// Call readCertificate() on the LacunaWebPKI object passing the selected certificate's thumbprint. This
		// reads the certificate's encoding, which we'll need later on. For more information, see
		// http://webpki.lacunasoftware.com/Help/classes/LacunaWebPKI.html#method_readCertificate
		pki.readCertificate($scope.selectedCertificate.thumbprint).success(function (encodedCert) {

			// Once the user's certificate encoding has been read, we sign the "nonce" data
			pki.signData({
				thumbprint: $scope.selectedCertificate.thumbprint,
				data: nonce,
				digestAlgorithm: 'SHA-256'
			}).success(function (signature) {
				onNonceSigned(nonce, encodedCert, signature);
			});
		});
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the signature of the "nonce" data is completed
	// -------------------------------------------------------------------------------------------------
	var onNonceSigned = function (nonce, encodedCert, signature) {
		$http.post('/Api/Authentication', {
			certificate: encodedCert,
			nonce: nonce,
			signature: signature
		}).then(onAuthSuccess, util.handleServerError);
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the server replies with the certificate of who 
	// is authenticating.
	// -------------------------------------------------------------------------------------------------
	var onAuthSuccess = function (response) {
		blockUI.stop();
		util.showMessage('Authentication successful!', 'Click OK to see the certificate details').result.then(function () {
			util.showCertificate(response.data.certificate);
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
