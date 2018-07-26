﻿var app = angular.module('PkiSdkWebApiSamples', ['ngRoute', 'ui.bootstrap', 'blockUI']);

app.config(['$routeProvider', function ($routeProvider) {

	$routeProvider.when('/', {
		templateUrl: 'app/views/home.html',
		controller: 'homeController'
    });

	$routeProvider.when('/authentication', {
		templateUrl: 'app/views/authentication.html',
		controller: 'authenticationController'
	});

	$routeProvider.when('/pades-signature', {
		templateUrl: 'app/views/pades-signature.html',
		controller: 'padesSignatureController'
	});

	$routeProvider.when('/cades-signature', {
		templateUrl: 'app/views/cades-signature.html',
		controller: 'cadesSignatureController'
	});

	$routeProvider.when('/xml-element-signature', {
		templateUrl: 'app/views/xml-element-signature.html',
		controller: 'xmlElementSignatureController'
    });

    $routeProvider.when('/open-xml-signature', {
        templateUrl: 'app/views/open-xml-signature.html',
        controller: 'openXmlSignatureController'
    });

	$routeProvider.otherwise({ redirectTo: "/" });
}]);

app.config(['blockUIConfig', function (blockUIConfig) {
	blockUIConfig.autoBlock = false;
	blockUIConfig.message = 'Please wait ...';
}]);

app.factory('util', ['$uibModal', 'blockUI', function ($modal, blockUI) {

	var showMessage = function (title, message) {
		return $modal.open({
			templateUrl: 'app/views/dialogs/message.html',
			controller: ['$scope', function ($scope) {
				$scope.title = title;
				$scope.message = message;
			}]
		});
	};

	var showSignatureResults = function (data) {
		return $modal.open({
			templateUrl: 'app/views/dialogs/signature-results.html',
			controller: 'signatureResultsDialogController',
			size: 'lg',
			resolve: {
				model: function () { return data.certificate; },
				filename: function () { return data.filename }
			}
		});
	};

	var showCertificate = function (model) {
		return $modal.open({
			templateUrl: 'app/views/dialogs/certificate.html',
			controller: 'certificateDialogController',
			size: 'lg',
			resolve: {
				model: function () { return model; }
			}
		});
	};

	var showValidationResults = function (vr) {
		return $modal.open({
			templateUrl: 'app/views/dialogs/validation-results.html',
			controller: 'validationResultsDialogController',
			size: 'lg',
			resolve: {
				model: function () { return vr; }
			}
		});
    };

	var handleServerError = function (response) {
		blockUI.stop();
		if (response.status === 400 && response.data.validationResults) {
			showMessage('Validation failed!', 'One or more validations failed. Click OK to see more details.').result.then(function () {
				showValidationResults(response.data.validationResults);
			});
		} else {
			showMessage('An error has occurred', response.data.message || 'HTTP error ' + response.status);
		}
	};

	return {
		showMessage: showMessage,
		showSignatureResults: showSignatureResults,
		showCertificate: showCertificate,
		showValidationResults: showValidationResults,
		handleServerError: handleServerError
	};
}]);