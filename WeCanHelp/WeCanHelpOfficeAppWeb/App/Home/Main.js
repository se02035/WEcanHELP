var angularApp = angular
    .module('app');
//.controller('Main', Main);

angularApp.controller('Main', ['$scope', '$http', '$location', 'adalAuthenticationService', function ($scope, $http, $location, adalService) {
    'use strict';

    $scope.login = function () {
        adalService.login();
    };
    $scope.logout = function () {
        adalService.logOut();
    };
    $scope.isActive = function (viewLocation) {
        return viewLocation === $location.path();
    };
    // optional
    $scope.$on("adal:loginSuccess", function () {
        //$scope.testMessage = "loginSuccess";
        loadData($http, $scope);
    });

    // optional
    $scope.$on("adal:loginFailure", function () {
        $scope.testMessage = "loginFailure";
        //$location.path("/login");
    });

    //// optional
    //$scope.$on("adal:notAuthorized", function (event, rejection, forResource) {
    //    $scope.testMessage = "It is not Authorized for resource:" + forResource;
    //});
    
    //loadData($http, $scope);

}]);

// Reads data from current document selection and displays a notification
function getDataFromSelection() {
    Office.context.document.getSelectedDataAsync(Office.CoercionType.Text,
        function (result) {
            if (result.status === Office.AsyncResultStatus.Succeeded) {
                app.showNotification('The selected text is:', '"' + result.value + '"');
                //$('#videoFrame').attr('src', '//aka.ms/azuremediaplayeriframe?url=%2F%2Famssamples.streaming.mediaservices.windows.net%2F91492735-c523-432b-ba01-faba6c2206a2%2FAzureMediaServicesPromo.ism%2Fmanifest&autoplay=false');
                return result.value;
            } else {
                app.showNotification('Error:', result.error.message);
                return "";
            }
        }
    );
}
//angularApp.controller("Main", function ($scope, $http) {
   

//});

function play(e) {
    $('#videoFrame').attr('src', 'https://aka.ms/azuremediaplayeriframe?autoplay=false&url=' + encodeURIComponent(e.href));
}

function loadData($http, $scope) {
    $http({
        method: 'GET',
        url: 'https://wecanhelphack.azurewebsites.net/odata/Assets?$filter=Application/Id%20eq%201%20and%20Published%20ne%20null'
    }).then(function successCallback(response) {
        // this callback will be called asynchronously
        // when the response is available
        $scope.videos = response.data.value;
    }, function errorCallback(response) {
        // called asynchronously if an error occurs
        // or server returns response with an error status.
        $scope.testMessage = response;
    });

}