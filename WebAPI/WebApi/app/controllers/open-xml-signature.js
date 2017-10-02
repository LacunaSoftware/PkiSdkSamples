'use strict';
app.controller('openXmlSignatureController', ['$scope', '$http', 'util', function ($scope, $http, util) {

    $scope.fileContent = null;
    $scope.signatures = null;

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
        if (this.files.length === 0) {
            return;
        }
        var file = this.files[0];
        var reader = new FileReader();
        reader.onload = function (event) {
            var content = event.target.result.split(',')[1];
            $scope.$apply(function () {
                $scope.fileContent = content;
            });
        };
        reader.readAsDataURL(file);
    };

    // -------------------------------------------------------------------------------------------------
    // Function called once the "Upload" button is clicked. This function will call the API to open
    // and validate the signatures on an existing XML file
    // -------------------------------------------------------------------------------------------------
    $scope.submit = function () {

        if ($scope.fileContent === null) {
            util.showMessage('Message', 'Please select a file');
            return;
        }

        $http.post('Api/OpenXmlSignature', {
            fileContent: $scope.fileContent
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