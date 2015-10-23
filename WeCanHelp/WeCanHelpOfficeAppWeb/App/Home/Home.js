/// <reference path="../App.js" />

(function () {
    "use strict";

    // The initialize function must be run each time a new page is loaded
    Office.initialize = function (reason) {
        $(document).ready(function () {
            app.initialize();
            $('#get-data-from-selection').click(getDataFromSelection);
        });
    };

    // Reads data from current document selection and displays a notification
    function getDataFromSelection() {
        Office.context.document.getSelectedDataAsync(Office.CoercionType.Text,
            function (result) {
                if (result.status === Office.AsyncResultStatus.Succeeded) {
                    app.showNotification('The selected text is:', '"' + result.value + '"');
                    //$('#videoFrame').attr('src', '//aka.ms/azuremediaplayeriframe?url=%2F%2Famssamples.streaming.mediaservices.windows.net%2F91492735-c523-432b-ba01-faba6c2206a2%2FAzureMediaServicesPromo.ism%2Fmanifest&autoplay=false');
                    var elem = angular.element(document.querySelector('[ng-app]'));
                    var injector = elem.injector();
                    var $http = injector.get('$http');
                    var $scope = injector.get('$scope');
                    loadData($http, $scope);
                } else {
                    app.showNotification('Error:', result.error.message);
                }
            }
        );
    }
})();

