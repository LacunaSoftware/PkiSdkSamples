'use strict';
app.controller('openXmlSignatureController', ['$scope', '$http', 'util', function ($scope, $http, util) {

    $scope.signatures = null;
    $scope.fileContent = null;

    // -------------------------------------------------------------------------------------------------
	// Function that renders the upload page
	// -------------------------------------------------------------------------------------------------
    var init = function () {

        // Set event handler for file selection
        $('#upload-input').change(onFileSelected);
        
        //$http.post('Api/OpenXmlSignature/' + userfile).then(onSignatureOpened, util.handleServerError);
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

    $scope.submit = function () {

        if ($scope.fileContent === null) {
            util.showMessage('Message', 'Please select a file');
            return;
        }

        $http.post('Api/OpenXmlSignature', {
            fileContent: $scope.fileContent
        }).then(onSignatureOpened, util.handleServerError);
    };

    // -------------------------------------------------------------------------------------------------
	// Function called once the server replies with the "signatures"
	// -------------------------------------------------------------------------------------------------
    var onSignatureOpened = function (signatures) {
        $scope.signatures = signatures;
    };
    
    init();

}]);