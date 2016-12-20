'use strict';
app.controller('authenticationController', ['$scope', '$http', 'blockUI', 'util', function ($scope, $http, blockUI, util) {

	$scope.certificates = [];
	$scope.selectedCertificate = null;

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

	$scope.signIn = function () {
		if ($scope.selectedCertificate == null) {
			util.showMessage('Message', 'Please select a certificate');
			return;
		}
		blockUI.start();
		$http.get('/Api/Authentication').then(onNonceAcquired, util.handleServerError);
	};

	var onNonceAcquired = function (response) {
		var nonce = response.data;
		pki.readCertificate($scope.selectedCertificate.thumbprint).success(function (encodedCert) {
			pki.signData({
				thumbprint: $scope.selectedCertificate.thumbprint,
				data: nonce,
				digestAlgorithm: 'SHA-256'
			}).success(function (signature) {
				onNonceSigned(nonce, encodedCert, signature);
			});
		});
	};

	var onNonceSigned = function (nonce, encodedCert, signature) {
		$http.post('/Api/Authentication', {
			certificate: encodedCert,
			nonce: nonce,
			signature: signature
		}).then(onAuthSuccess, util.handleServerError);
	};

	var onAuthSuccess = function (response) {
		blockUI.stop();
		util.showMessage('Authentication successful!', 'Click OK to see the certificate details').result.then(function () {
			util.showCertificate(response.data.certificate);
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
