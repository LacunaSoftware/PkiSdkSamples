'use strict';
app.controller('openXmlSignatureController', ['$scope', '$http', 'util', function ($scope, $http, util) {

    $scope.signatures = [];

    // Global variable to store uploaded file content
    var fileContent = null;

    // -------------------------------------------------------------------------------------------------
	// Function that renders the upload page
	// -------------------------------------------------------------------------------------------------
    var init = function () {
        // Set event handler for file selection
        $('#upload-input').change(onFileSelected);
    };

    // -------------------------------------------------------------------------------------------------
    // Function called once the file is selected
    // -------------------------------------------------------------------------------------------------
    function onFileSelected() {

        // Clear parameters when a new file is selected
        $scope.$apply(function () {
            fileContent = null;
            $scope.signatures = [];
        });

        if (this.files.length === 0) {
            return;
        }

        var file = this.files[0];
        var reader = new FileReader();
        reader.onload = function (event) {
            var content = event.target.result.split(',')[1];
            $scope.$apply(function () {
                fileContent = content;
            });
        };
        reader.readAsDataURL(file);
    };

    // -------------------------------------------------------------------------------------------------
    // Function called once the "Upload" button is clicked. This function will call the server to open
    // and validate the signatures on an existing XML file
    // -------------------------------------------------------------------------------------------------
    $scope.submit = function () {

        if (fileContent === null) {
            util.showMessage('Message', 'Please select a file');
            return;
        }

        $http.post('Api/OpenXmlSignature', {
            fileContent: fileContent
        }).then(function (response) {
            $scope.signatures = response.data.signatures;
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