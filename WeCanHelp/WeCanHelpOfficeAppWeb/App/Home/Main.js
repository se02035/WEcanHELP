var angularApp = angular
    .module('app');
//.controller('Main', Main);

angularApp.controller("Main", function ($scope, $http) {
    'use strict';

    $http({
        method: 'GET',
        url: 'http://localhost:61416/API/Values'
    }).then(function successCallback(response) {
        // this callback will be called asynchronously
        // when the response is available
        $scope.videos = response.data;
    }, function errorCallback(response) {
        // called asynchronously if an error occurs
        // or server returns response with an error status.
    });

});

function play(e) {
    $('#videoFrame').attr('src', e.href);
}