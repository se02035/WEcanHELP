(function () {
    'use strict';

    angular.module('app', [
        // Angular modules 
        'ngRoute',
        'AdalAngular'
    ])
    .config(['$routeProvider', '$httpProvider', 'adalAuthenticationServiceProvider', function ($routeProvider, $httpProvider, adalProvider) {

        $routeProvider.when("/Main", {
            controller: "Main",
            requireADLogin: true            
            //templateUrl: "/App/Views/Home.html"        
        }).when("/UserData", {
            controller: "userDataCtrl",
            templateUrl: "/App/Views/UserData.html",
            requireADLogin: true
        }).otherwise({ redirectTo: "/Main" });

        adalProvider.init(
            {
                instance: 'https://login.microsoftonline.com/',
                tenant: 'dabures.onmicrosoft.com',
                clientId: 'cca6fc21-991a-41e2-ba12-28948da8635c',
                extraQueryParameter: 'nux=1',
                //cacheLocation: 'localStorage', // enable this for IE, as sessionStorage does not work for localhost.
            },
            $httpProvider
            );

    }])
    ;
})();

