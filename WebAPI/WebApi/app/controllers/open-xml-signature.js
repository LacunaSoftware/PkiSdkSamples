'use strict';
app.controller('openXmlSignatureController', ['$scope', '$http', '$routeParams', 'blockUI', 'util', function ($scope, $http, $routeParams, blockUI, util) {

	$scope.signatures = [];

	// -------------------------------------------------------------------------------------------------
	// Function called when the page is rendered.
	// -------------------------------------------------------------------------------------------------
	var init = function () {

		// Block the UI while we get things ready.
		blockUI.start();

		// Call to Api/OpenXmlSignature to open the XML signature. We will render the signatures 
		// information through the "signature" $scope variable.
		$http.post('Api/OpenXmlSignature', {
			// Pass the fileId, received as a URL parameter filled by uploadController.
			fileId: $routeParams.fileId 
		}).then(function (response) {

			// Receive the signature models from the API.
			$scope.signatures = response.data.signatures;

			// Unblock the UI.
			blockUI.stop();

		}, util.handleServerError);

	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the "View certificate" button is clicked. This function will show the 
	// certificate infomations of the selected signature in a modal
	// -------------------------------------------------------------------------------------------------
	$scope.showValidationResults = function (vr) {
		util.showValidationResults(vr);
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once the "View validation results" button is clicked. This function will show 
	// the validation results of the selected signature in a modal
	// -------------------------------------------------------------------------------------------------
	$scope.showCertificate = function (cert) {
		util.showCertificate(cert);
	};

	init();

}]);