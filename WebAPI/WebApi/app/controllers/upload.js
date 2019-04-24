'use strict';
app.controller('uploadController', ['$scope', '$http', '$window', '$routeParams', 'blockUI', 'util', function ($scope, $http, $window, $routeParams, blockUI, util) {

	$scope.file = null;
	$scope.progress = null;

	var init = function () {

	};

	// -------------------------------------------------------------------------------------------------
	// Callback called once the uploaded is completed. We will redirect the user to /{rc}/{fileId} route
	// passing the uploaded file's id.
	// -------------------------------------------------------------------------------------------------
	var onUploadSuccess = function (response) {

		// Receive the file's id from server after it stored the file.
		var fileId = response.data;

		// Block the UI while the user is redirected.
		blockUI.start();

		// Set a timeout to show the blockUI message to user.
		setTimeout(function () {

			// Redirect the user.
			$window.location.href = '#/' + $routeParams.rc + '/' + encodeURI(fileId);

			// Unblock the UI.
			blockUI.stop();

		}, 1000);
	};

	// -------------------------------------------------------------------------------------------------
	// Callback to update the progress value using ProgressEvent.
	// -------------------------------------------------------------------------------------------------
	var onUploadProgress = function (pe) {

		// Show progress if the request has that information.
		if (pe.lengthComputable) {
			$scope.progress = Math.floor(pe.loaded / pe.total * 100);
		}
	};

	// -------------------------------------------------------------------------------------------------
	// Callback called when some error occurs during a upload.
	// -------------------------------------------------------------------------------------------------
	var onUploadFailure = function (err) {

		// Build error message.
		var errorMsg = 'An error ' + err.status;
		if (err.statusText) {
			errorMsg += ' (' + err.statusText + ')';
		}
		errorMsg += err.message ? ': ' + err.message : '.';

		// Show error.
		util.showMessage('Error', errorMsg);
	};

	// -------------------------------------------------------------------------------------------------
	// Function called once a file is uploaded by user.
	// -------------------------------------------------------------------------------------------------
	$scope.upload = function () {

		// Create request's body using FormData class.
		var body = new FormData();
		body.append('file', $scope.file);
		body.append('name', $scope.file.name);
		body.append('contentType', $scope.file.type);

		// Perform request to UploadController.
		$http({
			method: 'POST',
			url: '/Api/Upload',
			data: body,
			headers: {
				// You must specify this header with 'undefined', because it lets the browser to decide
				// what 'Content-Type' to use. This is necessary because the default behavior is to use
				// 'application/json' and the request will fail when arrives on server.
				'Content-Type': undefined
			},
			uploadEventHandlers: { progress: onUploadProgress }
		}).then(onUploadSuccess, onUploadFailure);
	};

	init();

}]);