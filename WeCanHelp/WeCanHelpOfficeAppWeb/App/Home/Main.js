var angularApp = angular
    .module('app');
//.controller('Main', Main);

angularApp.controller("Main", function ($scope, $http) {
    'use strict';

    $http({
        method: 'GET',
        url: 'http://localhost:30883/odata/Assets?$filter=Application/Id%20eq%201%20and%20Published%20ne%20null'
    }).then(function successCallback(response) {
        // this callback will be called asynchronously
        // when the response is available
        $scope.videos = response.data.value;
    }, function errorCallback(response) {
        // called asynchronously if an error occurs
        // or server returns response with an error status.
    });

});

function play(e) {
    $('#videoFrame').attr('src', 'https://aka.ms/azuremediaplayeriframe?autoplay=false&url=' + encodeURIComponent(e.href));
}